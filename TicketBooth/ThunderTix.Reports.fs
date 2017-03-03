namespace OpenMind.TicketBooth.ThunderTix

open System

open Microsoft.FSharp.Linq

open FSharp.Data

open OpenMind.TicketBooth.Sql

module Reports =

    // TODO atomicity
    let ingestBarcodeReport (Csv report) =

        let reportOrders = report.Rows |> Seq.groupBy (fun row -> row.Columns.[0].AsInteger()) |> Map.ofSeq
        let existingOrdersSeq = query {
            for order in database.Main.Orders do
                select order.ThunderTixOrderId
        }
        let existingOrderIds = Set.ofSeq existingOrdersSeq
        let reportOrderIds = Map.keys reportOrders
        let newOrderIds = Set.difference reportOrderIds existingOrderIds
        let newOrders = Map.filter (fun k v -> newOrderIds.Contains k) reportOrders

        for KVP (orderId, tickets) in newOrders do
            let orderId = int orderId
            let t = (Seq.head tickets).Columns
            
            let order_row = database.Main.Orders.Create ()
            order_row.ThunderTixOrderId <- int t.[0]
            order_row.FirstName <- Some t.[1]
            order_row.LastName <- Some t.[2]
            let cleaned_up_date_time = t.[8].Replace("-", "") |> DateTime.Parse
            order_row.PurchaseTime <- Some cleaned_up_date_time
            order_row.Email <- Some t.[10]

            database.SubmitUpdates()
            
            for ticket in tickets do
                let t = ticket.Columns
                let ticket_row = database.Main.Tickets.Create ()
                ticket_row.OrderId <- order_row.OrderId
                ticket_row.TicketType <- Some t.[3]
                ticket_row.Barcode <- Some t.[5]

            database.SubmitUpdates()

       