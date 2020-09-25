namespace Fluke.UI.Frontend.Components

open System
open Browser
open Fable.Core
open Browser.Types
open FSharpPlus
open Fable.React
open Suigetsu.UI.Frontend.React
open Suigetsu.Core
open Feliz
open Feliz.Recoil
open Feliz.UseListener
open Fluke.UI.Frontend
open Fluke.Shared
open Fable.Core.JsInterop


module MainComponent =
    open Domain.Information
    open Domain.UserInteraction
    open Domain.State

    let globalShortcutHandler =
        React.memo (fun () ->
            let selection, setSelection = Recoil.useState Recoil.Selectors.selection
            let ctrlPressed, setCtrlPressed = Recoil.useState Recoil.Atoms.ctrlPressed
            let shiftPressed, setShiftPressed = Recoil.useState Recoil.Atoms.shiftPressed

            let keyEvent (e: KeyboardEvent) =
                if e.ctrlKey <> ctrlPressed then
                    setCtrlPressed e.ctrlKey

                if e.shiftKey <> shiftPressed then
                    setShiftPressed e.shiftKey

                if e.key = "Escape" && not selection.IsEmpty then
                    setSelection Map.empty

            React.useListener.onKeyDown keyEvent
            React.useListener.onKeyUp keyEvent

            nothing)

    let positionUpdater =
        React.memo (fun () ->
            let resetPosition = Recoil.useResetState Recoil.Selectors.position

            Scheduling.useScheduling Scheduling.Interval (60 * 1000) resetPosition
            //        Scheduling.useScheduling Scheduling.Interval (10 * 1000) resetPosition

            nothing)

    let dataLoader =
        React.memo (fun () ->
            let view = Recoil.useValue Recoil.Selectors.view

            let loadState =
                Recoil.useCallbackRef (fun setter ->
                    async {
                        Recoil.Profiling.addTimestamp "dataLoader.loadStateCallback[0]"
                        let! state = setter.snapshot.getAsync (Recoil.Selectors.stateAsync view)

                        printfn "dataLoader.state=None:%A" (state.IsNone)

                        Ext.setDom (nameof state) state

                        Recoil.Profiling.addTimestamp "dataLoader.loadStateCallback[1]"

                        setter.set (Recoil.Selectors.state, state)

                        Recoil.Profiling.addTimestamp "dataLoader.loadStateCallback[2]"
                    }
                    |> Async.StartImmediate)

            Recoil.Profiling.addTimestamp "dataLoader render"
            React.useEffect
                ((fun () ->
                    Recoil.Profiling.addTimestamp "dataLoader effect"
                    loadState ()),

                 // TODO: return a cleanup?
                 [|
                     view :> obj
                 |])

            nothing)

    let soundPlayer =
        React.memo (fun (input: {| Username: Username |}) ->
            let oldActiveSessions = React.useRef []
            let (Minute sessionLength) = Recoil.useValue (Recoil.Atoms.RecoilUser.sessionLengthFamily input.Username)

            let (Minute sessionBreakLength) =
                Recoil.useValue (Recoil.Atoms.RecoilUser.sessionBreakLengthFamily input.Username)

            let activeSessions = Recoil.useValue Recoil.Selectors.activeSessions

            React.useEffect
                ((fun () ->
                    oldActiveSessions.current
                    |> List.map (fun (Model.ActiveSession (oldTaskName, (Minute oldDuration), _, _)) ->
                        let newSession =
                            activeSessions
                            |> List.tryFind (fun (Model.ActiveSession (taskName, (Minute duration), _, _)) ->
                                taskName = oldTaskName
                                && duration = oldDuration + 1.)

                        match newSession with
                        | Some (Model.ActiveSession (_, (Minute newDuration), _, _)) when oldDuration = -1.
                                                                                          && newDuration = 0. ->
                            Temp.Sound.playTick
                        | Some (Model.ActiveSession (_, newDuration, totalDuration, _)) when newDuration = totalDuration ->
                            Temp.Sound.playDing
                        | None ->
                            if oldDuration = sessionLength
                               + sessionBreakLength
                               - 1. then
                                Temp.Sound.playDing
                            else
                                fun () -> ()
                        | _ -> fun () -> ())
                    |> List.iter (fun x -> x ())

                    oldActiveSessions.current <- activeSessions),
                 [|
                     sessionLength :> obj
                     sessionBreakLength :> obj
                     activeSessions :> obj
                 |])

            nothing)

    let autoReload_TEMP =
        React.memo (fun () ->
            let reload = React.useCallback (fun () -> Dom.window.location.reload true)

            Scheduling.useScheduling Scheduling.Timeout (60 * 60 * 1000) reload

            nothing)

    let diag =
        React.memo (fun () ->
            let text, setText = React.useState ""
            let oldJson, setOldJson = React.useState ""
            let debug = Recoil.useValue Recoil.Atoms.debug

            Scheduling.useScheduling Scheduling.Interval 100 (fun () ->
                if not debug then
                    ()
                else
                    let indent n = String (' ', n)

                    let json =
                        Recoil.Profiling.profilingState
                        |> Fable.SimpleJson.SimpleJson.stringify
                        |> JS.JSON.parse
                        |> fun obj -> JS.JSON.stringify (obj, unbox null, 4)
                        |> String.replace (sprintf ",\n%s" (indent 3)) ""
                        |> String.replace (indent 1) ""
                        |> String.replace "][\n" ""
                        |> String.replace "\"" " "

                    if json = oldJson then
                        ()
                    else
                        setText json
                        setOldJson json)

            if not debug then
                nothing
            else
                React.fragment [
                    Html.pre [
                        prop.id "diag"
                        prop.style [
                            style.custom ("width", "min-content")
                            style.custom ("height", "80%")
                            style.position.fixedRelativeToWindow
                            style.right 0
                            style.bottom 0
                            style.fontSize 9
                            style.backgroundColor "#44444488"
                            style.zIndex 1
                        ]
                        prop.children
                            [
                                str text
                            ]
                    ]

                    Html.div [
                        prop.id "test1"
                        prop.style [
                            style.position.absolute
                            style.width 100
                            style.height 100
                            style.top 0
                            style.right 0
                            style.backgroundColor "#ccc3"
                            style.zIndex 1
                        ]
                        prop.children
                            [
                                str "test1"
                            ]
                    ]
                ])

    let render =
        React.memo (fun () ->
            let username = Recoil.useValue Recoil.Atoms.username

            React.fragment [
                diag ()

                match username with
                | Some username ->
                    globalShortcutHandler ()
                    positionUpdater ()
                    autoReload_TEMP ()

                    React.suspense
                        ([
                            dataLoader ()
                            soundPlayer {| Username = username |}

                            NavBarComponent.render {| Username = username |}
                            PanelsComponent.render ()
                         ],
                         PageLoaderComponent.render ())

                | None -> str "no user"
            ])
