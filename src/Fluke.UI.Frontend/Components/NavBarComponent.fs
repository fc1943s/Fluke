namespace Fluke.UI.Frontend.Components

open Browser
open Feliz.Router
open Browser.Types
open FSharpPlus
open Fable.React
open Fable.React.Props
open Fulma
open Feliz
open Feliz.Recoil
open Feliz.Bulma
open Feliz.UseListener
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.Model
open Fluke.Shared


module NavBarComponent =
    open Domain.Information
    open Domain.UserInteraction
    open Domain.State
    open View

    let render =
        React.memo (fun (input: {| Username: Username |}) ->
            let debug, setDebug = Recoil.useState Recoil.Atoms.debug
            let view = Recoil.useValue Recoil.Selectors.view
            let activeSessions = Recoil.useValue (Recoil.Selectors.Session.activeSessions input.Username)

            let setView view =
                let path =
                    Router.formatPath [|
                        "view"
                        string view
                    |]

                Dom.window.location.href <- path


            React.useListener.onKeyDown (fun (e: KeyboardEvent) ->
                match e.ctrlKey, e.shiftKey, e.key with
                | _, true, "C" -> setView View.View.Calendar
                | _, true, "G" -> setView View.View.Groups
                | _, true, "T" -> setView View.View.Tasks
                | _, true, "W" -> setView View.View.Week
                | _ -> ())

            //        Bulma.navbar [
//            prop.children [
//            ]
//
//        ]
            Navbar.navbar [
                              Navbar.Color IsBlack
                          ] [

                let checkbox isChecked text onClick =
                    Bulma.navbarItem.div [
                        prop.className "field"
                        prop.onClick (fun _ -> onClick ())
                        prop.style [
                            style.marginBottom 0
                            style.alignSelf.center
                        ]
                        prop.children [
                            Checkbox.input [
                                CustomClass "switch is-small is-dark"
                                Props [
                                    Checked isChecked
                                    OnChange (fun _ -> ())
                                ]
                            ]

                            Checkbox.checkbox [] [ str text ]
                        ]
                    ]

                let viewCheckbox newView text = checkbox (view = newView) text (fun () -> setView newView)

                viewCheckbox View.View.Calendar "calendar view"
                viewCheckbox View.View.Groups "groups view"
                viewCheckbox View.View.Tasks "tasks view"
                viewCheckbox View.View.Week "week view"
                checkbox debug "debug" (fun () -> setDebug (not debug))

                TreeSelectorComponent.render {| Username = input.Username |}

                Bulma.navbarItem.div
                    [
                        activeSessions
                        |> List.map (fun (ActiveSession (taskName,
                                                         (Minute duration),
                                                         (Minute totalDuration),
                                                         (Minute totalBreakDuration))) ->
                            let sessionType, color, duration, left =
                                let left = totalDuration - duration
                                match duration < totalDuration with
                                | true -> "Session", "#7cca7c", duration, left
                                | false -> "Break", "#ca7c7c", -left, totalBreakDuration + left

                            Chakra.box
                                {| color = color |}
                                [
                                    sprintf
                                        "%s: Task[ %s ]; Duration[ %.1f ]; Left[ %.1f ]"
                                        sessionType
                                        taskName
                                        duration
                                        left
                                    |> str
                                ])
                        |> List.intersperse (br [])
                        |> function
                        | [] -> str "No active session"
                        | list -> ofList list
                    ]
            ])