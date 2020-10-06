namespace Fluke.UI.Frontend.Components

open Fable.React
open Feliz
open Feliz.UseListener
open Feliz.Recoil
open FSharpPlus
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Bindings
open Fluke.Shared

module DayComponent =
    open Domain.Information
    open Domain.UserInteraction
    open Domain.State

    let render =
        React.memo (fun (input: {| Username: Username
                                   Date: FlukeDate
                                   Label: string |}) ->
            let isToday = Recoil.useValue (Recoil.Selectors.FlukeDate.isToday input.Date)
            let hasSelection = Recoil.useValue (Recoil.Selectors.FlukeDate.hasSelection input.Date)
            let weekStart = Recoil.useValue (Recoil.Atoms.User.weekStart input.Username)

            Chakra.box
                {|
                    color =
                        if hasSelection then
                            "#ff5656"
                        elif isToday then
                            "#777"
                        else
                            ""
                    borderLeft =
                        match (weekStart, input.Date) with
                        | StartOfMonth -> "1px solid #ffffff3d"
                        | StartOfWeek -> "1px solid #222"
                        | _ -> ""
                    className = Css.cellSquare
                |}
                [
                    str <| String.toLower input.Label
                ])
