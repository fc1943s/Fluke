namespace Fluke.UI.Frontend.Components

open Fable.React
open Feliz
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.State


module Checkbox =
    [<ReactComponent>]
    let Checkbox (label: string option) (props: UI.IChakraProps -> unit) =
        let darkMode = Store.useValue Atoms.User.darkMode

        UI.checkbox
            (fun x ->
                x.colorScheme <- "purple"
                x.borderColor <- if darkMode then "#484848" else "#b7b7b7"
                x.size <- "lg"
                x.alignSelf <- "flex-start"
                props x)
            [
                match label with
                | Some label ->
                    UI.box
                        (fun x -> x.fontSize <- "main")
                        [
                            str label
                        ]
                | _ -> nothing
            ]
