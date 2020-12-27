namespace Fluke.UI.Frontend

open Fable.Remoting.Client
open Fluke.Shared

module Sync =
    open Sync

    let api (baseUrl: string option) =
        Remoting.createApi ()
        |> Remoting.withBinarySerialization
        |> Remoting.withBaseUrl
            (baseUrl
             |> Option.defaultValue $"https://fc1943s.github.io:{serverPort}")
        |> Remoting.buildProxy<Api>

    let handleRequest fn =
        async {
            let! result = fn |> Async.Catch

            return
                match result with
                | Choice1Of2 output -> Some output
                | Choice2Of2 ex ->
                    match ex with
                    | :? ProxyRequestException as ex ->
                        let _response: HttpResponse = ex.Response
                        let responseText: string = ex.ResponseText
                        let statusCode: int = ex.StatusCode

                        printfn $"Proxy exception: {ex.Message}. responseText={responseText}; statusCode={statusCode}"
                    | ex -> printfn $"API exception: {ex}"

                    None
        }
