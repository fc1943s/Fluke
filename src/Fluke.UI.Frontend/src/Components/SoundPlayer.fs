namespace Fluke.UI.Frontend.Components

open Fable.React
open Feliz
open Feliz.UseListener
open Fluke.UI.Frontend
open Fluke.UI.Frontend.State
open Fluke.Shared.Domain
open Fluke.UI.Frontend.Bindings

module SoundPlayer =

    open Model
    open UserInteraction

    [<ReactComponent>]
    let SoundPlayer (input: {| Username: Username |}) =
        let oldActiveSessions = React.useRef []
        let (Minute sessionDuration) = Store.useValue (Atoms.User.sessionDuration input.Username)
        let (Minute sessionBreakDuration) = Store.useValue (Atoms.User.sessionBreakDuration input.Username)
        let activeSessions = Store.useValueLoadableDefault (Selectors.Session.activeSessions input.Username) []

        React.useEffect (
            (fun () ->
                oldActiveSessions.current
                |> List.map
                    (fun (TempUI.ActiveSession (oldTaskName, Minute oldDuration, _, _)) ->
                        let newSession =
                            activeSessions
                            |> List.tryFind
                                (fun (TempUI.ActiveSession (taskName, Minute duration, _, _)) ->
                                    taskName = oldTaskName
                                    && duration = oldDuration + 1)

                        match newSession with
                        | Some (TempUI.ActiveSession (_, Minute newDuration, _, _)) when
                            oldDuration = -1 && newDuration = 0 -> TempAudio.playTick
                        | Some (TempUI.ActiveSession (_, newDuration, totalDuration, _)) when
                            newDuration = totalDuration -> TempAudio.playDing
                        | None ->
                            if oldDuration = sessionDuration + sessionBreakDuration - 1 then
                                TempAudio.playDing
                            else
                                id
                        | _ -> id)
                |> List.iter (fun x -> x ())

                oldActiveSessions.current <- activeSessions),
            [|
                box oldActiveSessions
                box sessionDuration
                box sessionBreakDuration
                box activeSessions
            |]
        )

        nothing
