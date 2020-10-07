namespace Fluke.UI.Frontend.Components

open Fable.React
open Feliz
open Feliz.UseListener
open Fable.DateFunctions
open Fluke.Shared
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Bindings
open Feliz.Recoil


module MonthResponsiveCellComponent =
    open Domain.Information
    open Domain.UserInteraction
    open Domain.State

    let render =
        React.memo (fun (input: {| Username: Username
                                   Date: FlukeDate
                                   Props: {| width: int |} |}) ->
            let weekStart = Recoil.useValue (Recoil.Atoms.User.weekStart input.Username)
            let month = input.Date.DateTime.Format "MMM"

            Chakra.box
                {| input.Props with
                    textAlign = "center"
                    borderLeft =
                        match (weekStart, input.Date) with
                        | StartOfMonth -> "1px solid #ffffff3d"
                        | _ -> ""
                |}
                [
                    str month
                ])