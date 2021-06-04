namespace Fluke.UI.Frontend.Components

open System
open Browser.Types
open Fable.React
open Feliz
open Fluke.UI.Frontend.Bindings
open Feliz.Recoil
open Fable.Core
open Fluke.Shared


module Input =

    [<RequireQualifiedAccess>]
    type InputFormat =
        | Date
        | Time
        | DateTime
        | Email
        | Password
        | Number

    type IProps<'TValue, 'TKey> =
        inherit Chakra.IChakraProps

        abstract hint : ReactElement option with get, set
        abstract hintTitle : ReactElement option with get, set
        abstract atom : Recoil.InputAtom<'TValue, 'TKey> option with get, set
        abstract inputScope : Recoil.InputScope<'TValue> option with get, set
        abstract value : 'TValue option with get, set
        abstract onFormat : ('TValue -> string) option with get, set
        abstract onValidate : (string * 'TValue option -> 'TValue option) option with get, set
        abstract onEnterPress : (_ -> JS.Promise<unit>) option with get, set
        abstract inputFormat : InputFormat option with get, set


    [<ReactComponent>]
    let Input (props: IProps<'TValue, 'TKey> -> unit) =
        let props = JS.newObj props

        let atomFieldOptions = Recoil.useAtomFieldOptions<'TValue, 'TKey> props.atom props.inputScope

        let inputFallbackRef = React.useRef<HTMLInputElement> null

        let inputRef =
            props.ref
            |> Option.ofObjUnbox
            |> Option.defaultValue inputFallbackRef

        let mounted, setMounted = React.useState false

        let currentValue, currentValueString =
            React.useMemo (
                (fun () ->
                    let value =
                        match mounted, props.value with
                        | _, Some value -> Some value
                        | false, None -> None
                        | true, None ->
                            match inputRef.current, box atomFieldOptions.AtomValue with
                            | null, _ -> None
                            | _, null ->
                                match props.onValidate with
                                | Some onValidate ->
                                    onValidate (inputRef.current.value, Some atomFieldOptions.AtomValue)
                                | None -> None
                            | _ ->
                                match props.atom with
                                | Some _ -> Some atomFieldOptions.AtomValue
                                | None -> None


                    let valueString =
                        match value with
                        | Some value when box value <> null ->
                            match props.onFormat with
                            | Some onFormat -> onFormat value
                            | None -> string value
                        | _ -> ""

                    value, valueString),
                [|
                    box mounted
                    box props
                    box inputRef
                    box atomFieldOptions.AtomValue
                |]
            )

        let fireChange =
            Recoil.useCallbackRef
                (fun _ ->
                    inputRef.current.dispatchEvent (Dom.createEvent "change" {| bubbles = true |})
                    |> ignore)

        React.useEffect (
            (fun () ->
                match inputRef.current with
                | null -> ()
                | _ ->
                    inputRef.current.value <- currentValueString

                    if not mounted then
                        if props.atom.IsSome then fireChange ()

                        setMounted true),
            [|
                box fireChange
                box props
                box inputRef
                box currentValueString
                box mounted
                box setMounted
            |]
        )

        let onChange =
            Recoil.useCallbackRef
                (fun _setter (e: KeyboardEvent) ->
                    promise {
                        if inputRef.current <> null && e.target <> null then
                            match box props.onChange with
                            | null -> ()
                            | _ -> do! props.onChange e

                            let validValue =
                                match props.onValidate with
                                | Some onValidate ->
                                    let validValue = onValidate (e.Value, currentValue)
                                    validValue
                                | None -> Some (box e.Value :?> 'TValue)

                            let validValueString =
                                match validValue with
                                | Some validValue ->
                                    match props.onFormat with
                                    | Some onFormat -> onFormat validValue
                                    | None -> string validValue
                                | None -> ""

                            if validValueString <> currentValueString then
                                inputRef.current.value <- validValueString

                            if props.atom.IsSome then
                                match validValue with
                                | Some value -> atomFieldOptions.SetAtomValue value
                                | None -> atomFieldOptions.SetAtomValue atomFieldOptions.ReadOnlyValue
                    })

        Chakra.stack
            (fun x ->
                x.spacing <- "5px"
                x.flex <- "1")
            [
                match props.label with
                | null -> nothing
                | _ ->
                    InputLabel.InputLabel
                        {|
                            Hint = props.hint
                            HintTitle = props.hintTitle
                            Label = props.label
                            Props = fun _ -> ()
                        |}

                Chakra.box
                    (fun x ->
                        x.position <- "relative"
                        x.flex <- "1")
                    [
                        Chakra.input
                            (fun x ->

                                x.onChange <- onChange
                                x.ref <- inputRef
                                x._focus <- JS.newObj (fun x -> x.borderColor <- "heliotrope")

                                x.autoFocus <- props.autoFocus

                                props.placeholder
                                |> Chakra.mapIfSet (fun value -> x.placeholder <- value)
                                |> ignore

                                props.width
                                |> Chakra.mapIfSet (fun value -> x.width <- value)
                                |> ignore

                                props.paddingLeft
                                |> Chakra.mapIfSet (fun value -> x.paddingLeft <- value)
                                |> ignore

                                x.onKeyDown <-
                                    fun (e: KeyboardEvent) ->
                                        promise {
                                            match box props.onKeyDown with
                                            | null -> ()
                                            | _ -> do! props.onKeyDown e

                                            match box props.onChange with
                                            | null -> ()
                                            | _ -> do! props.onChange e

                                            match props.onEnterPress with
                                            | Some onEnterPress -> if e.key = "Enter" then do! onEnterPress ()
                                            | None -> ()
                                        }

                                x.``type`` <-
                                    match props.inputFormat with
                                    | Some inputFormat ->
                                        match inputFormat with
                                        | InputFormat.Date -> "date"
                                        | InputFormat.Time -> "time"
                                        | InputFormat.Number -> "number"
                                        | InputFormat.DateTime -> "datetime-local"
                                        | InputFormat.Email -> "email"
                                        | InputFormat.Password -> "password"
                                    | None -> null)
                            []

                        match props.inputFormat with
                        | Some InputFormat.Number ->
                            Chakra.stack
                                (fun x ->
                                    x.position <- "absolute"
                                    x.right <- "1px"
                                    x.top <- "0"
                                    x.height <- "100%"
                                    x.borderLeftWidth <- "1px"
                                    x.borderLeftColor <- "#484848"
                                    x.spacing <- "0")
                                [
                                    let numberButtonClick (value: string) (op: float -> float) =
                                        match Double.TryParse value with
                                        | true, value ->
                                            match props.onValidate with
                                            | Some onValidate ->
                                                match onValidate (string (op value), currentValue) with
                                                | Some value -> inputRef.current.valueAsNumber <- unbox value
                                                | None -> ()
                                            | None -> inputRef.current.valueAsNumber <- op value
                                        | _ -> ()

                                    Button.Button
                                        {|
                                            Hint = None
                                            Icon = Some (Icons.fa.FaSortUp |> Icons.wrap, Button.IconPosition.Left)
                                            Props =
                                                fun x ->
                                                    x.height <- "50%"
                                                    x.paddingTop <- "6px"
                                                    x.borderRadius <- "0 5px 0 0"
                                                    x.minWidth <- "26px"

                                                    x.onClick <-
                                                        (fun _ ->
                                                            promise {
                                                                numberButtonClick inputRef.current.value ((+) 1.)
                                                                fireChange ()
                                                            })
                                            Children = []
                                        |}

                                    Button.Button
                                        {|
                                            Hint = None
                                            Icon = Some (Icons.fa.FaSortDown |> Icons.wrap, Button.IconPosition.Left)
                                            Props =
                                                fun x ->
                                                    x.height <- "50%"
                                                    x.paddingBottom <- "6px"
                                                    x.borderRadius <- "0 0 5px 0"
                                                    x.minWidth <- "26px"

                                                    x.onClick <-
                                                        (fun _ ->
                                                            promise {
                                                                numberButtonClick
                                                                    inputRef.current.value
                                                                    (fun n -> n - 1.)

                                                                fireChange ()
                                                            })
                                            Children = []
                                        |}
                                ]
                        | _ -> nothing
                    ]
            ]

    let inline LeftIconInput icon props =
        Chakra.inputGroup
            (fun x -> x.display <- "flex")
            [
                Chakra.inputLeftElement
                    (fun _ -> ())
                    [
                        icon
                    ]

                Input
                    (fun x ->
                        x.paddingLeft <- "25px"

                        props x)

            ]
