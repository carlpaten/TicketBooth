open System
open System.Threading

open Suave
open Suave.Authentication
open Suave.Filters
open Suave.Operators
open Suave.RequestErrors
open Suave.Successful

[<EntryPoint>]
let main argv = 
    let app =
            authenticateBasic (fun (x, y) -> x = y) (
                choose 
                    [
                        GET >=> OK "Hello World"
                    ]
            )
    let cts = new CancellationTokenSource()
    let x = HttpBinding.create
    let conf = 
        { 
            defaultConfig with 
                cancellationToken = cts.Token 
                bindings = [ HttpBinding.create Protocol.HTTP Net.IPAddress.Any 8080us ]
        }
    let listening, server = startWebServerAsync conf app
    Async.Start(server, cts.Token)
    printfn "Make requests now"
    Console.ReadKey true |> ignore
    cts.Cancel()
    0 // return an integer exit code
