namespace Fluke.UI.Frontend.Components

open Fable.React
open Feliz
open Fluke.UI.Frontend.State
open Fluke.UI.Frontend.Bindings
open Fluke.Shared

module Day =
    open Domain.UserInteraction

    [<ReactComponent>]
    let Day (date: FlukeDate) (label: string) =
        let isToday = Store.useValue (Selectors.FlukeDate.isToday date)
        let hasCellSelection = Store.useValue (Selectors.FlukeDate.hasCellSelection date)
        let weekStart = Store.useValue Atoms.weekStart
        let cellSize = Store.useValue Atoms.cellSize

        Chakra.box
            (fun x ->
                x.color <-
                    if hasCellSelection then "#ff5656"
                    elif isToday then "#777"
                    else null

                x.borderLeftWidth <-
                    match (weekStart, date) with
                    | StartOfMonth
                    | StartOfWeek -> "1px"
                    | _ -> null

                x.borderLeftColor <-
                    match (weekStart, date) with
                    | StartOfMonth -> "#ffffff3d"
                    | StartOfWeek -> "#222"
                    | _ -> null

                x.whiteSpace <- "nowrap"
                x.height <- $"{cellSize}px"
                x.width <- $"{cellSize}px"
                x.lineHeight <- $"{cellSize}px"
                x.textAlign <- "center")
            [
                str (String.toLower label)
            ]
