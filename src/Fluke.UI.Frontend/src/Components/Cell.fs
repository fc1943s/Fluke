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
        (input: {| TaskIdAtom: Store.Atom<TaskId>
                   DateIdAtom: Store.Atom<DateId>
                   SemiTransparent: bool |})
        =
        Profiling.addCount "- CellComponent.render"

        let taskId = Store.useValue input.TaskIdAtom
        let dateId = Store.useValue input.DateIdAtom
        let cellSize = Store.useValue Atoms.User.cellSize
        let isReadWrite = Store.useValue (Selectors.Task.isReadWrite taskId)
        let sessionStatus = Store.useValue (Selectors.Cell.sessionStatus (taskId, dateId))
        let attachmentIdSet = Store.useValue (Selectors.Cell.attachmentIdSet (taskId, dateId))
        let isToday = Store.useValue (Selectors.DateId.isToday dateId)
        let selected = Store.useValue (Selectors.Cell.selected (taskId, dateId))
        let setSelected = Setters.useSetSelected ()
        let cellUIFlag = Store.useValue (Atoms.User.uiFlag UIFlagType.Cell)
        let rightDock = Store.useValue Atoms.User.rightDock
        let deviceInfo = Store.useValue Selectors.deviceInfo

        let cellColorDisabled = Store.useValue Atoms.User.cellColorDisabled
        let cellColorSuggested = Store.useValue Atoms.User.cellColorSuggested
        let cellColorPending = Store.useValue Atoms.User.cellColorPending
        let cellColorMissed = Store.useValue Atoms.User.cellColorMissed
        let cellColorMissedToday = Store.useValue Atoms.User.cellColorMissedToday
        let cellColorPostponedUntil = Store.useValue Atoms.User.cellColorPostponedUntil
        let cellColorPostponed = Store.useValue Atoms.User.cellColorPostponed
        let cellColorCompleted = Store.useValue Atoms.User.cellColorCompleted
        let cellColorDismissed = Store.useValue Atoms.User.cellColorDismissed
        let cellColorScheduled = Store.useValue Atoms.User.cellColorScheduled

        let onCellClick =
            Store.useCallback (
                (fun getter _ (e: MouseEvent) ->
                    promise {
                        if deviceInfo.IsTesting then
                            let ctrlPressed = Store.value getter Atoms.ctrlPressed
                            let shiftPressed = Store.value getter Atoms.shiftPressed

                            let newSelected =
                                if ctrlPressed || shiftPressed then
                                    taskId, dateId, not selected
                                else
                                    taskId, dateId, false

                            do! setSelected newSelected
                        else
                            let newSelected = if e.ctrlKey || e.shiftKey then not selected else false

                            if selected <> newSelected then
                                do! setSelected (taskId, dateId, newSelected)
                    }),
                [|
                    box deviceInfo
                    box taskId
                    box dateId
                    box selected
                    box setSelected
                |]
            )

        UI.center
            (fun x ->
                UI.setTestId
                    x
                    $"cell-{taskId}-{(dateId |> DateId.Value |> FlukeDate.DateTime)
                                         .ToShortDateString ()}"

                if isReadWrite then x.onClick <- onCellClick
                x.width <- $"{cellSize}px"
                x.height <- $"{cellSize}px"
                x.lineHeight <- $"{cellSize}px"
                x.position <- "relative"

                x.backgroundColor <-
                    (match sessionStatus with
                     | Disabled -> cellColorDisabled
                     | Suggested -> cellColorSuggested
                     | Pending -> cellColorPending
                     | Missed -> cellColorMissed
                     | MissedToday -> cellColorMissedToday
                     | UserStatus (_, status) ->
                         match status with
                         | Completed -> cellColorCompleted
                         | Postponed until -> if until.IsSome then cellColorPostponedUntil else cellColorPostponed
                         | Dismissed -> cellColorDismissed
                         | Scheduled -> cellColorScheduled
                     |> Color.Value)
                    + (if isToday then "aa"
                       elif input.SemiTransparent then "d9"
                       else "")

                x.textAlign <- "center"

                x.borderWidth <- "1px"

                x.borderColor <- if selected then "#ffffffAA" else "transparent"

                if isReadWrite then
                    x._hover <- JS.newObj (fun x -> x.borderColor <- "#ffffff55")
                    x.cursor <- "pointer")
            [
                match rightDock, cellUIFlag with
                | Some TempUI.DockType.Cell, UIFlag.Cell (taskId', dateId') when taskId' = taskId && dateId' = dateId ->
                    UI.icon
                        (fun x ->
                            x.``as`` <- Icons.ti.TiPin
                            x.fontSize <- $"{cellSize - 4}px"
                            x.color <- "white")
                        []
                | _ -> nothing

                CellSessionIndicator.CellSessionIndicator input.TaskIdAtom input.DateIdAtom

                if selected then
                    nothing
                else
                    CellBorder.CellBorder input.TaskIdAtom input.DateIdAtom

                CellStatusUserIndicator.CellStatusUserIndicator input.TaskIdAtom input.DateIdAtom

                if not attachmentIdSet.IsEmpty then
                    AttachmentIndicator.AttachmentIndicator ()
                else
                    nothing
            ]

    [<ReactComponent>]
    let CellWrapper
        (input: {| TaskIdAtom: Store.Atom<TaskId>
                   DateIdAtom: Store.Atom<DateId>
                   SemiTransparent: bool |})
        =
        let taskId = Store.useValue input.TaskIdAtom
        let dateId = Store.useValue input.DateIdAtom
        let enableCellPopover = Store.useValue Atoms.User.enableCellPopover
        let isReadWrite = Store.useValue (Selectors.Task.isReadWrite taskId) //
        let navigate = Navigate.useNavigate ()

        if enableCellPopover then
            Popover.CustomPopover //
                {|
                    CloseButton = false //
                    Padding = Some "3px" //
                    Props = fun x -> x.placement <- "right-start" //
                    Trigger = Cell input
                    Body =
                        fun (disclosure, _fetchInitialFocusRef) ->
                            [ //
                                if isReadWrite then //
                                    CellMenu.CellMenu
                                        input.TaskIdAtom
                                        input.DateIdAtom
                                        (Some disclosure.onClose) // None
                                        true
                                else //
                                    nothing //
                            ] //
                |} //
        else //
            UI.box
                (fun x ->
                    x.onClick <-
                        fun _ ->
                            promise {
                                do!
                                    navigate (
                                        Navigate.DockPosition.Right,
                                        Some TempUI.DockType.Cell,
                                        UIFlagType.Cell,
                                        UIFlag.Cell (taskId, dateId)
                                    )
                            })
                [
                    Cell input
                ]
