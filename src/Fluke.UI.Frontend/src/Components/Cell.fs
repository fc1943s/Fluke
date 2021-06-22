namespace Fluke.UI.Frontend.Components

open Fable.Core
open Fable.React
open Fable.Core.JsInterop
open Feliz
open Fluke.Shared.Domain.Model
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Bindings
open Fluke.Shared.Domain
open Fluke.UI.Frontend.Hooks


module Cell =
    open UserInteraction
    open State

    [<ReactComponent>]
    let Cell
        (input: {| Username: Username
                   TaskId: TaskId
                   DateId: DateId
                   SemiTransparent: bool |})
        =
        Profiling.addCount "- CellComponent.render"

        let cellSize = Store.useValue (Atoms.User.cellSize input.Username)
        let isTesting = Store.useValue Atoms.isTesting
        let showUser = Store.useValue (Selectors.Task.showUser (input.Username, input.TaskId))
        let isReadWrite = Store.useValue (Selectors.Task.isReadWrite (input.Username, input.TaskId))
        let sessionStatus = Store.useValue (Selectors.Cell.sessionStatus (input.Username, input.TaskId, input.DateId))
        let sessions = Store.useValue (Selectors.Cell.sessions (input.Username, input.TaskId, input.DateId))
        let attachments = Store.useValue (Selectors.Cell.attachments (input.Username, input.TaskId, input.DateId))
        let isToday = Store.useValue (Selectors.FlukeDate.isToday (input.DateId |> DateId.Value))
        let selected = Store.useValue (Selectors.Cell.selected (input.Username, input.TaskId, input.DateId))
        let setSelected = Setters.useSetSelected ()

        let onCellClick =
            Store.useCallback (
                (fun get set _ ->
                    promise {
                        let ctrlPressed = Atoms.getAtomValue get Atoms.ctrlPressed
                        let shiftPressed = Atoms.getAtomValue get Atoms.shiftPressed

                        let newSelected =
                            if ctrlPressed || shiftPressed then
                                input.Username, input.TaskId, input.DateId, not selected
                            else
                                input.Username, input.TaskId, input.DateId, false

                        do! setSelected newSelected
                    }),
                [|
                    box input
                    box selected
                    box setSelected
                |]
            )

        Popover.CustomPopover
            {|
                CloseButton = false
                Padding = "3px"
                Placement = Some "right-start"
                Trigger =
                    Chakra.center
                        (fun x ->
                            if isTesting then
                                x?``data-testid`` <- $"cell-{input.TaskId}-{
                                                                                (input.DateId
                                                                                 |> DateId.Value
                                                                                 |> FlukeDate.DateTime)
                                                                                    .ToShortDateString ()
                                }"

                            if isReadWrite then x.onClick <- onCellClick
                            x.width <- $"{cellSize}px"
                            x.height <- $"{cellSize}px"
                            x.lineHeight <- $"{cellSize}px"
                            x.position <- "relative"

                            x.backgroundColor <-
                                (TempUI.cellStatusColor sessionStatus)
                                + (if isToday then "aa"
                                   elif input.SemiTransparent then "d9"
                                   else "")

                            x.textAlign <- "center"

                            x.borderColor <- if selected then "#ffffffAA" else "transparent"

                            x.borderWidth <- "1px"

                            if isReadWrite then
                                x.cursor <- "pointer"
                                x._hover <- JS.newObj (fun x -> x.borderColor <- "#ffffff55"))
                        [

                            CellSessionIndicator.CellSessionIndicator
                                {|
                                    Status = sessionStatus
                                    Sessions = sessions
                                |}

                            if selected then
                                nothing
                            else
                                CellBorder.CellBorder
                                    {|
                                        Username = input.Username
                                        TaskId = input.TaskId
                                        Date = input.DateId |> DateId.Value
                                    |}

                            match showUser, sessionStatus with
                            | true, UserStatus (username, _manualCellStatus) ->
                                CellStatusUserIndicator.CellStatusUserIndicator {| Username = username |}
                            | _ -> nothing

                            AttachmentIndicator.AttachmentIndicator
                                {|
                                    Username = input.Username
                                    Attachments = attachments
                                |}
                        ]
                Body =
                    fun (disclosure, _initialFocusRef) ->
                        [
                            if isReadWrite && not isTesting then
                                CellMenu.CellMenu
                                    {|
                                        Username = input.Username
                                        TaskId = input.TaskId
                                        DateId = input.DateId
                                        OnClose = disclosure.onClose
                                    |}
                            else
                                nothing
                        ]
            |}
