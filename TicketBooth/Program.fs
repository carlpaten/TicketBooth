namespace OpenMind.TicketBooth

open System
open System.Collections.Generic
open System.IO
open System.Text
open System.Threading

open FSharp.Data

open Suave
open Suave.Authentication
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.Successful

module Program =

    [<EntryPoint>]
    let main argv = 
        let service = new ThunderTix.Service (argv.[0], argv.[1])

        let test (ctx: HttpContext) = 
            match ctx.request.formData "event_name", ctx.request.formData "barcode" with
            | _, Choice2Of2 _ -> BAD_REQUEST (sprintf "Parameter `barcode` required") ctx
            | Choice2Of2 _, _ -> BAD_REQUEST (sprintf "Parameter `event_name` required") ctx
            | Choice1Of2 event_name, Choice1Of2 barcode -> 
                let user_name = string ctx.userState.["userName"]
                let result = service.Scan user_name event_name barcode
                OK (sprintf "%A" result) ctx

        // from Suave
        let parseAuthenticationToken (token : string) =
            let parts = token.Split (' ')
            let enc = parts.[1].Trim()
            let bytes = Convert.FromBase64String enc
            let decoded = Encoding.ASCII.GetString bytes
            let indexOfColon = decoded.IndexOf(':')
            (parts.[0].ToLower(), decoded.Substring(0,indexOfColon), decoded.Substring(indexOfColon+1))

        let app =
            authenticateBasic service.LogIn (
                choose 
                    [
                        GET >=> OK "Hello World"
                        POST >=> path "/scan" >=> test
                    ]
            )

        
        let cts = new CancellationTokenSource()
        let conf = { defaultConfig with cancellationToken = cts.Token }
        let listening, server = startWebServerAsync conf app
        Async.Start(server, cts.Token)
        printfn "Make requests now"
        Console.ReadKey true |> ignore
        cts.Cancel()
        0