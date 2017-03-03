namespace OpenMind.TicketBooth

open FSharp.Data.Sql

module Sql =

    #if DEBUG
    let [<Literal>] private connectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"\bin\Debug\sqlite.db;Version=3;foreign keys=true"
    #else
    let [<Literal>] private connectionString = "Data Source=" + __SOURCE_DIRECTORY__ + @"\bin\Release\sqlite.db;Version=3;foreign keys=true"
    #endif

    #if DEBUG
    let [<Literal>] private resolutionPath = __SOURCE_DIRECTORY__ + @"\bin\Debug"
    #else
    let [<Literal>] private resolutionPath = __SOURCE_DIRECTORY__ + @"\bin\Release"
    #endif

    // create a type alias with the connection string and database vendor settings
    type Sql = SqlDataProvider< 
                  ConnectionString = connectionString,
                  DatabaseVendor = Common.DatabaseProviderTypes.SQLITE,
                  ResolutionPath = resolutionPath,
                  IndividualsAmount = 1000,
                  UseOptionTypes = true >

    let database = Sql.GetDataContext()