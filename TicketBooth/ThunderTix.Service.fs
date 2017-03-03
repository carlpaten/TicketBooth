namespace OpenMind.TicketBooth.ThunderTix

open System
open System.Collections.Generic
open System.Threading

open FSharp.Data

type Report = Csv of CsvFile

type ScanResult =
    | AlreadyScanned of DateTime * string
    | Success of string
    | NotFound


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
            let csv_parsed = CsvFile.Parse csv_string

            // delete CSV file from server
            let id = response.["download"].["download"].["id"].AsString ()
            let response = service_session.PostHtml (sprintf "/downloads/%s" id, ["_method", "delete"])
            
            // return report
            Csv csv_parsed

    member x.LogIn (user_name, password) =
        try
            sessions.[user_name] <- new Session(user_name, password)
            true
        with
        | e -> false

    member x.GetOrders event_name =
        // First perform a search, then ask for the search results in CSV form.
        let search_response =
            service_session.PostHtml ("/all_orders",
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

    member x.GetBarcodes event_name =
        let perf = performance event_name
        let response = service_session.GetHtml (sprintf "/reports/barcode_export/%s" perf.Id)
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

