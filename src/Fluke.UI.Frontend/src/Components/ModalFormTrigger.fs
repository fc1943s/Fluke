namespace Fluke.UI.Frontend.Components

open Feliz
open Fable.Core
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.State
open Fable.React


module ModalFormTrigger =
    [<ReactComponent>]
    let ModalFormTrigger
        (input: {| UIFlagType: Atoms.UIFlagType
                   UIFlagValue: Atoms.UIFlag
                   Trigger: (unit -> JS.Promise<unit>) -> Store.GetFn * Store.SetFn -> ReactElement |})
        =
        let onTrigger =
            Store.useCallback (
                (fun _ setter _ ->
                    promise {
                        Store.set setter (Atoms.uiFlag input.UIFlagType) input.UIFlagValue
                        Store.set setter (Atoms.uiVisibleFlag input.UIFlagType) true
                    }),
                [||]
            )

        let callbacks = Store.useCallbacks ()
        let content, setContent = React.useState nothing

        React.useEffect (
            (fun () ->
                promise {
                    let! callbacks = callbacks ()
                    setContent (input.Trigger onTrigger callbacks)
                }
                |> Promise.start),
            [|
                box input.Trigger
                box onTrigger
                box callbacks
            |]
        )

        content
