namespace Fluke.UI.Frontend.Components

open Browser.Types
open Fable.Core
open Fable.React
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
        (input: {| TaskId: TaskId
                   DateId: DateId
                   SemiTransparent: bool |})
        =
        Profiling.addCount "- CellComponent.render"

        let cellSize = Store.useValue Atoms.User.cellSize
        let showUser = Store.useValue (Selectors.Task.showUser input.TaskId)
        let isReadWrite = Store.useValue (Selectors.Task.isReadWrite input.TaskId)
        let sessionStatus = Store.useValue (Selectors.Cell.sessionStatus (input.TaskId, input.DateId))
        let sessions = Store.useValue (Selectors.Cell.sessions (input.TaskId, input.DateId))
        let attachmentIdSet = Store.useValue (Selectors.Cell.attachmentIdSet (input.TaskId, input.DateId))
        let isToday = Store.useValue (Selectors.FlukeDate.isToday (input.DateId |> DateId.Value))
        let selected = Store.useValue (Selectors.Cell.selected (input.TaskId, input.DateId))
        let setSelected = Setters.useSetSelected ()
        let cellUIFlag = Store.useValue (Atoms.User.uiFlag UIFlagType.Cell)
        let rightDock = Store.useValue Atoms.User.rightDock
        let deviceInfo = Store.useValue Selectors.deviceInfo

        let onCellClick =
            Store.useCallback (
                (fun getter _ (e: MouseEvent) ->
                    promise {
                        if deviceInfo.IsTesting then
                            let ctrlPressed = Store.value getter Atoms.ctrlPressed
                            let shiftPressed = Store.value getter Atoms.shiftPressed

                            let newSelected =
                                if ctrlPressed || shiftPressed then
                                    input.TaskId, input.DateId, not selected
                                else
                                    input.TaskId, input.DateId, false

                            do! setSelected newSelected
                        else
                            let newSelected = if e.ctrlKey || e.shiftKey then not selected else false

                            if selected <> newSelected then
                                do! setSelected (input.TaskId, input.DateId, newSelected)
                    }),
                [|
                    box deviceInfo
                    box input.TaskId
                    box input.DateId
                    box selected
                    box setSelected
                |]
            )

        UI.center
            (fun x ->
                UI.setTestId
                    x
                    $"cell-{input.TaskId}-{
                                               (input.DateId |> DateId.Value |> FlukeDate.DateTime)
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

                match rightDock, cellUIFlag with
                | Some TempUI.DockType.Cell, UIFlag.Cell (taskId, dateId) when
                    taskId = input.TaskId && dateId = input.DateId ->
                    UI.icon
                        (fun x ->
                            x.``as`` <- Icons.ti.TiPin
                            x.fontSize <- $"{cellSize - 4}px"
                            x.color <- "white")
                        []
                | _ -> nothing

                CellSessionIndicator.CellSessionIndicator sessionStatus sessions

                if selected then
                    nothing
                else
                    CellBorder.CellBorder input.TaskId (input.DateId |> DateId.Value)

                match showUser, sessionStatus with
                | true, UserStatus (_username, _manualCellStatus) -> CellStatusUserIndicator.CellStatusUserIndicator ()
                | _ -> nothing

                if not attachmentIdSet.IsEmpty then
                    AttachmentIndicator.AttachmentIndicator ()
                else
                    nothing
            ]

    [<ReactComponent>]
    let CellWrapper
        (input: {| TaskId: TaskId
                   DateId: DateId
                   SemiTransparent: bool |})
        =
        let enableCellPopover = Store.useValue Atoms.User.enableCellPopover
        let isReadWrite = Store.useValue (Selectors.Task.isReadWrite input.TaskId)
        let setRightDock = Store.useSetState Atoms.User.rightDock
        let setCellUIFlag = Store.useSetState (Atoms.User.uiFlag UIFlagType.Cell)

        if enableCellPopover then
            Popover.CustomPopover
                {|
                    CloseButton = false
                    RenderOnHover = false
                    Padding = "3px"
                    Props = fun x -> x.placement <- "right-start"
                    Trigger = Cell input
                    Body =
                        fun (disclosure, _initialFocusRef) ->
                            [
                                if isReadWrite then
                                    CellMenu.CellMenu input.TaskId input.DateId (Some disclosure.onClose) true
                                else
                                    nothing
                            ]
                |}
        else
            UI.box
                (fun x ->
                    x.onClick <-
                        fun _ ->
                            promise {
                                setRightDock (Some TempUI.DockType.Cell)
                                setCellUIFlag (UIFlag.Cell (input.TaskId, input.DateId))
                            })
                [
                    Cell input
                ]
