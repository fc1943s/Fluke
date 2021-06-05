namespace Fluke.UI.Frontend.Components

open System
open Fable.React
open Feliz
open Feliz.Recoil
open Fluke.Shared.Domain.UserInteraction
open Fluke.UI.Frontend.State
open Fluke.UI.Frontend.Hooks
open Fluke.UI.Frontend.Bindings


module PositionUpdater =

    [<ReactComponent>]
    let PositionUpdater (input: {| Username: Username |}) =
        let isTesting = Recoil.useValue Atoms.isTesting
        let position, setPosition = Recoil.useState Atoms.position
        let selectedDatabaseIdList = Recoil.useValue (Atoms.User.selectedDatabaseIdList input.Username)

        let selectedDatabasePositions =
            selectedDatabaseIdList
            |> List.map (fun databaseId -> Atoms.Database.position (input.Username, databaseId))
            |> Recoil.waitForAll
            |> Recoil.useValue

        let pausedPosition =
            selectedDatabasePositions
            |> List.choose id
            |> List.tryHead

        let run =
            Recoil.useCallbackRef
                (fun _ ->
                    promise {
                        match selectedDatabasePositions, pausedPosition with
                        | [], _ -> if position <> None then setPosition None
                        | _, None ->
                            let newPosition = FlukeDateTime.FromDateTime DateTime.Now

                            if (not isTesting || position.IsNone)
                               && Some newPosition <> position then
                                printfn $"Updating position newPosition={newPosition |> FlukeDateTime.Stringify}"
                                setPosition (Some newPosition)
                        | _, Some _ ->
                            if position <> pausedPosition then
                                printfn
                                    $"Updating position selectedDatabasePositions.[0]={
                                                                                           pausedPosition
                                                                                           |> Option.map
                                                                                               FlukeDateTime.Stringify
                                    }"

                                setPosition pausedPosition
                    })

        React.useEffect (
            (fun () -> run () |> Promise.start),
            [|
                box run
            |]
        )

        Scheduling.useScheduling Scheduling.Interval 1000 (fun _ -> run ())

        nothing
