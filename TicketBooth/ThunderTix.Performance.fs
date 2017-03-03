namespace OpenMind.TicketBooth.ThunderTix

open FSharp.Data

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