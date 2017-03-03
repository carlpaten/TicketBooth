namespace OpenMind.TicketBooth.ThunderTix

open System.IO
open System.Net

open FSharp.Data

type private Session (user_name, password) =

    let cc = CookieContainer()

    let get_text_body (r: HttpResponse) =
        match r.Body with
        | Text body -> body
        | other -> impossible ()

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
                headers = [ HttpRequestHeaders.Accept HttpContentTypes.Json ],
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