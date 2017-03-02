module Sql

open FSharp.Data
open FSharp.Data.Sql

open System
open System.Data.SQLite
open System.IO
open System.Text
open System.Text.RegularExpressions

let db_file_name = "test.sqlite"

type TicketType = Type of string

type Ticket = {
    Type: TicketType
    BarCode: string
}

type Address = {
    ``Address Line 1``: string
    ``Address Line 2``: string
    City: string
    State: string
    Country: string
    ``ZIP Code``: string
}

type Customer = {
    ``First Name``: string
    ``Last Name``: string
    ``Email Address``: string
    ``Phone Number``: string 
}

type Order = {
    ``Order Number``: int
    ``Date of Purchase``: DateTime
    Tickets: Ticket list
    // TODO: payment method, comments, staff comments, ...
}

let parse_dt date time =
    let mutable dt = DateTime.Parse(date)
    let time_match = Regex.Match(time, @"^([0-1]?[0-9]):([0-9]{2}) (AM|PM)")
    let data = [ for g in time_match.Groups -> g.Captures.[0].Value ] |> List.tail
    dt <- 
        match data.[2] with
        | "AM" -> dt
        | "PM" -> dt.AddHours(12.0)
        | other -> impossible ()
    dt <- dt.AddHours   (float data.[0])
    dt <- dt.AddMinutes (float data.[1])

    dt

let parse_tickets ticketString =
    let ticketString = "1 - Admission GÃ©nÃ©rale"
    let ticket_match = Regex.Match(ticketString, @"^([0-9]+) - (.*)$")
    let data = [ for g in ticket_match.Groups -> g.Captures.[0].Value ] |> List.tail
    List.init (int data.[0]) (fun _ -> { Type = Type data.[1]; BarCode = "" })

let row_to_order (row: string list) =
    {
        ``Order Number`` = int row.[0]
        ``Date of Purchase`` = parse_dt row.[1] row.[2]
        Tickets = parse_tickets row.[5] // row.[5]
    }

//let create_db (csv: string) =
//    SQLiteConnection.CreateFile(db_file_name)
//    use conn = new SQLiteConnection(sprintf "Data Source=%s;Version=3;" db_file_name)
//    conn.Open()
//    use comm = new SQLiteCommand(create_orders_table, conn)
//    let result = comm.ExecuteNonQuery ()
//    ()

//let load_csv (csv: string) =
//    use conn = new SQLiteConnection(sprintf "Data Source=%s;Version=3;" db_file_name)
//    conn.Open()
//
//    use r = new StringReader(csv)
//    let orders = CsvFile.Load(r).Cache().Rows
//    let count = Seq.length orders
//
//    for order in orders do
//        let sql = order.Columns |> Seq.map (sprintf "\"%s\"") |> String.concat ", " |> sprintf "INSERT INTO Orders VALUES (%s)"
//        use comm = new SQLiteCommand(sql, conn)
//        let result = comm.ExecuteNonQuery ()
//        ()
//    ()

//let test csv =
//    if not (File.Exists csv) then
//        create_db csv
//
//    use conn = new SQLiteConnection(sprintf "Data Source=%s;Version=3;" db_file_name)
//    conn.Open()
//
//    use r = new StringReader(csv)
//    let orders = CsvFile.Load(r).Cache().Rows
//    let count = Seq.length orders
//
//    for order in orders do
//        let sql = order.Columns |> Seq.map (sprintf "\"%s\"") |> String.concat ", " |> sprintf "INSERT INTO Orders VALUES (%s)"
//        use comm = new SQLiteCommand(sql, conn)
//        let result = comm.ExecuteNonQuery ()
//        ()
//    ()
//
//    ()

//let [<Literal>] resolutionPath = __SOURCE_DIRECTORY__ + @"..\..\files\sqlite" 
//let [<Literal>] connectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"\northwindEF.db;Version=3"
//
//type sql = SqlDataProvider< 
//              ConnectionString = connectionString,
//              DatabaseVendor = Common.DatabaseProviderTypes.SQLITE,
//              ResolutionPath = resolutionPath,
//              IndividualsAmount = 1000,
//              UseOptionTypes = true >
//
//
//let ctx = sql.GetDataContext()
//
//ctx.

//let christina = ctx.Main.Customers.Individuals.``As ContactName``.``BERGS, Christina Berglund``
//
//// directly enumerate an entity's relationships, 
//// this creates and triggers the relevant query in the background
//let christinasOrders = christina.``main.Orders by CustomerID`` |> Seq.toArray
//
//let mattisOrderDetails =
//    query { for c in ctx.Main.Customers do
//            // you can directly enumerate relationships with no join information
//            for o in c.``main.Orders by CustomerID`` do
//            // or you can explicitly join on the fields you choose
//            join od in ctx.Main.OrderDetails on (o.OrderId = od.OrderId)
//            //  the (!!) operator will perform an outer join on a relationship
//            for prod in (!!) od.``main.Products by ProductID`` do 
//            // nullable columns can be represented as option types; the following generates IS NOT NULL
//            where o.ShipCountry.IsSome                
//            // standard operators will work as expected; the following shows the like operator and IN operator
//            where (c.ContactName =% ("Matti%") && c.CompanyName |=| [|"Squirrelcomapny";"DaveCompant"|] )
//            sortBy o.ShipName
//            // arbitrarily complex projections are supported
//            select (c.ContactName,o.ShipAddress,o.ShipCountry,prod.ProductName,prod.UnitPrice) } 
//    |> Seq.toArray