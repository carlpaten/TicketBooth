namespace OpenMind

open FSharp.Data
open FSharp.Data.HttpRequestHeaders

open System
open System.Collections.Generic
open System.IO
open System.Net
open System.Text.RegularExpressions
open System.Threading

module ThunderTix =

    let get_text_body (r: HttpResponse) =
        match r.Body with
        | Text body -> body
        | other -> impossible ()

    type Report = Csv of string

    type ScanResult =
        | AlreadyScanned of DateTime * string
        | Success of string
        | NotFound

    type private Session (user_name, password) =
        let cc = CookieContainer()

        let csrf_token =
            let landing_page = 
                Http.Request (
                    url = "https://admin.thundertix.com", 
                    cookieContainer = cc
                )

            let body = get_text_body landing_page
            use reader = new StringReader(body)
            let document = HtmlDocument.Load(reader)
            let meta_elements = document.Descendants "meta"
            let k = meta_elements |> Seq.choose (fun element -> 
                match element.TryGetAttribute "name" with
                | Some attr when attr.Value() = "csrf-token" -> Some ((element.Attribute "content").Value ())
                | _ -> None
            )
            Seq.head k

        let login_response = 
            Http.Request (
                url = "https://admin.thundertix.com/session", 
                body = FormValues [
                    "utf8", "✓"
                    "authenticity_token", csrf_token
                    "login", user_name
                    "password", password
                    "commit", "Log in"
                ],
                cookieContainer = cc
            )

        member x.GetHtml endpoint =
            let response = 
                Http.Request (
                    url = "https://admin.thundertix.com" + endpoint,
                    cookieContainer = cc
                )

            let body =
                match response.Body with
                | Text body -> body
                | other -> impossible ()

            use reader = new StringReader(body)
            let document = HtmlDocument.Load(reader)
            document

        member x.GetJson endpoint = 
            let response = 
                Http.Request (
                    url = "https://admin.thundertix.com/downloads",
                    headers = [ Accept HttpContentTypes.Json ],
                    cookieContainer = cc
                )
            let body = get_text_body response
            let json = JsonValue.Parse(body)
            json

        member x.PostHtml (endpoint, body) =
            let response =
                Http.Request (
                    url = "https://admin.thundertix.com" + endpoint,
                    body = FormValues
                        [
                            yield "utf8", "✓"
                            yield "authenticity_token", csrf_token
                            yield! body
                        ],
                    cookieContainer = cc
                )

            let body =
                match response.Body with
                | Text body -> body
                | other -> impossible ()

            use reader = new StringReader(body)
            let document = HtmlDocument.Load(reader)
            document

    type private Performance (event_name, service_session: Session) =
        // TODO verify that this gets cached
        member val Id =
            let events_scanner_page = service_session.GetHtml "/barcode/events/"
            let event_button = events_scanner_page.CssSelect("a.btn") |> Seq.find (fun elem -> elem.InnerText () = event_name)
            let event_url = event_button.Attribute "href" |> HtmlAttributeExtensions.Value

            let event_page = service_session.GetHtml event_url
            let performance_button = event_page.CssSelect "a" |> Seq.find (fun elem -> elem.InnerText () = event_name)
            let performance_url = performance_button.Attribute "href" |> HtmlAttributeExtensions.Value
            let performance_id = regex_find @"^/barcode/reader\?performance_id=([0-9]+)" performance_url
            performance_id

        // Currently useless
        member val VenueId =
            let events_page = service_session.GetHtml "/events"
            let event_cell = 
                events_page.CssSelect("tr")
                |> List.find(fun row -> 
                    let event_name_cells = row.CssSelect("span.event-name")
                    event_name_cells.Length = 1 && event_name_cells.[0].InnerText() = event_name)
            let event_url = event_cell.CssSelect("a.btn").[0].Attribute "href" |> HtmlAttributeExtensions.Value
            let venueId = regex_find @"^/venues/([0-9]+)/events/[0-9]+$" event_url
            venueId

    type Service (user_name, password) =
        let sessions = Dictionary<_, _> ()

        let service_session =
            let s = new Session(user_name, password)
            sessions.Add(user_name, s)
            s
        
        let performance = memoize (fun event_name -> new Performance(event_name, service_session))

        let rec get_report () =
            Thread.Sleep 1000
            let response = service_session.GetJson("/downloads")
            if response.["download"].["download"].["resource_file_name"] = JsonValue.Null then
                get_report ()
            else
                // retrieve CSV file
                let url = response.["url"].AsString ()
                let csv_string = Http.RequestString (url)

                // delete CSV file from server
                let id = response.["download"].["download"].["id"].AsString ()
                let response = service_session.PostHtml (sprintf "/downloads/%s" id, ["_method", "delete"])

                // return report
                Csv csv_string
        member x.LogIn (user_name, password) =
            try
                sessions.[user_name] <- new Session(user_name, password)
                true
            with
            | e -> false

        member x.GetCsv credentials event_name =
            if true then
                Csv <| File.ReadAllText("..\..\orders.csv")
            else
                let session = sessions.[credentials]
                // First perform a search, then ask for the search results in CSV form.
                let search_response =
                    session.PostHtml ("/all_orders",
                        [
                            "search_order_number", ""
                            "search_start_date", "February 22, 2017 12:00 AM"
                            "search_end_date", "March 01, 2017 11:59 PM"
                            "commit", "Search"
                            "search_payment_type", ""
                            "search_agent", ""
                            "search_first_name", ""
                            "search_last_name", ""
                            "search_email", ""
                            "search_event", event_name
                            "include_totals", "on"
                            "show_tix_totals", "on"
                        ])
                // Ask for CSV results
                let csv_generate_response = service_session.GetHtml("/orders/orders_csv")

                let report = get_report ()
                report

        
        member x.Scan user_name event_name barcode =
            let session = sessions.[user_name]
            let performance = performance event_name
            let scan_response = 
                session.PostHtml("/barcode/reader", 
                    [
                        "bar[code]", barcode
                        "barcode_search", "Scan"
                        "performance_id", performance.Id
                    ])
            let answer = scan_response.CssSelect(".answer > div").[0].InnerText().Split([| "\r\n" |], StringSplitOptions.RemoveEmptyEntries)
            match answer.[0] with
            | RegexContains "ALREADY SCANNED" _ ->
                let dt = answer.[1].Replace(" EST", "") |> DateTime.Parse
                let rep = regex_find @"by (.*) $" answer.[2]
                AlreadyScanned (dt, rep)
            | RegexContains "SUCCESS!" _ ->
                let typ = answer.[1]
                Success typ
            | other -> impossible ()
        