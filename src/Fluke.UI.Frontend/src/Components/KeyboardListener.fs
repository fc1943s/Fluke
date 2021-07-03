namespace Fluke.UI.Frontend.Components

open Browser.Types
open Fable.Core.JsInterop
open Feliz
open Fable.React
open Fluke.UI.Frontend.State
open Fluke.UI.Frontend.Hooks
open Fable.Core
open Fluke.Shared
open Fluke.UI.Frontend.Bindings
open System
open Fluke.Shared.Domain.UserInteraction


module CtrlListener =
    [<ReactComponent>]
    let CtrlListener () =
        Listener.useKeyPress
            [|
                "Control"
            |]
            (fun getter setter e ->
                promise {
                    let ctrlPressed = Store.value getter Atoms.ctrlPressed

                    if e.ctrlKey <> ctrlPressed then
                        Store.set setter Atoms.ctrlPressed e.ctrlKey
                })

        nothing


module ShiftListener =
    [<ReactComponent>]
    let ShiftListener () =
        Listener.useKeyPress
            [|
                "Shift"
            |]
            (fun getter setter e ->
                promise {
                    let shiftPressed = Store.value getter Atoms.shiftPressed

                    if e.shiftKey <> shiftPressed then
                        Store.set setter Atoms.shiftPressed e.shiftKey
                })

        Listener.useKeyPress
            [|
                "I"
                "H"
                "P"
                "B"
            |]
            (fun _ setter e ->
                promise {
                    let setView value = Store.set setter Atoms.User.view value

                    match e.ctrlKey, e.altKey, e.key with
                    | true, true, "I" ->
                        JS.log (fun () -> "RouterObserver.onKeyDown() View.Information")
                        setView View.View.Information
                    | true, true, "H" -> setView View.View.HabitTracker
                    | true, true, "P" -> setView View.View.Priority
                    | true, true, "B" -> setView View.View.BulletJournal
                    | _ -> ()
                })


        nothing


module SelectionListener =
    [<ReactComponent>]
    let SelectionListener () =
        Listener.useKeyPress
            [|
                "Escape"
                "R"
            |]
            (fun getter setter e ->
                promise {
                    let cellSelectionMap = Store.value getter Selectors.Session.cellSelectionMap

                    if e.key = "Escape" && e.``type`` = "keydown" then
                        if not cellSelectionMap.IsEmpty then
                            cellSelectionMap
                            |> Map.keys
                            |> Seq.iter (fun taskId -> Store.set setter (Atoms.Task.selectionSet taskId) Set.empty)

                    if e.key = "R" && e.``type`` = "keydown" then
                        if not cellSelectionMap.IsEmpty then
                            let newMap =
                                if cellSelectionMap.Count = 1 then
                                    cellSelectionMap
                                    |> Map.toList
                                    |> List.map
                                        (fun (taskId, dates) ->
                                            let date =
                                                dates
                                                |> Seq.item (Random().Next (0, dates.Count - 1))

                                            taskId, Set.singleton date)
                                    |> Map.ofSeq
                                else
                                    let key =
                                        cellSelectionMap
                                        |> Map.keys
                                        |> Seq.item (Random().Next (0, cellSelectionMap.Count - 1))

                                    Map.singleton key cellSelectionMap.[key]

                            newMap
                            |> Map.iter
                                (fun taskId dates ->
                                    Store.set setter (Atoms.Task.selectionSet taskId) (dates |> Set.map DateId))
                })

        nothing
