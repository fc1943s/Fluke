namespace Fluke.UI.Frontend.Components

open Browser.Types
open Fable.DateFunctions
open Fable.React
open Feliz
open Fluke.Shared.Domain
open Fluke.Shared.Domain.Model
open Fluke.Shared.Domain.State
open Fluke.Shared.Domain.UserInteraction
open Fluke.UI.Frontend.Bindings
open System
open Fable.Core
open Fluke.UI.Frontend.Hooks
open Fluke.UI.Frontend.State
open Fluke.Shared
open Fluke.UI.Frontend.TempUI


module TaskForm =
    let useStartSession () =
        Store.useCallback (
            (fun getter setter taskId ->
                promise {
                    let sessions = Store.value getter (Atoms.Task.sessions taskId)

                    Store.set
                        setter
                        (Atoms.Task.sessions taskId)
                        (Session (
                            (let now = DateTime.Now in if now.Second < 30 then now else now.AddMinutes 1.)
                            |> FlukeDateTime.FromDateTime
                         )
                         :: sessions)
                }),
            [||]
        )

    [<ReactComponent>]
    let MissedAfterSelector taskId =
        let dayStart = Store.useValue Atoms.User.dayStart

        let tempMissedAfter =
            Store.Hooks.useTempAtom
                (Some (Store.InputAtom (Store.AtomReference.Atom (Atoms.Task.missedAfter taskId))))
                (Some (Store.InputScope.Temp Gun.defaultSerializer))

        UI.box
            (fun x -> x.display <- "inline")
            [
                InputLabel.InputLabel
                    {|
                        Hint = None
                        HintTitle = None
                        Label = str "Missed After"
                        Props = fun x -> x.marginBottom <- "5px"
                    |}

                UI.stack
                    (fun x ->
                        x.direction <- "row"
                        x.spacing <- "15px")
                    [
                        Checkbox.Checkbox
                            (if tempMissedAfter.Value.IsNone then Some "Enable" else None)
                            (fun x ->
                                x.isChecked <- tempMissedAfter.Value.IsSome
                                x.alignSelf <- "center"

                                x.onChange <-
                                    fun _ ->
                                        promise {
                                            tempMissedAfter.SetValue (
                                                if tempMissedAfter.Value.IsSome then None else (Some dayStart)
                                            )
                                        })

                        match tempMissedAfter.Value with
                        | Some missedAfter ->
                            Input.Input
                                {|
                                    CustomProps =
                                        fun x ->
                                            x.fixedValue <- Some missedAfter
                                            x.onFormat <- Some FlukeTime.Stringify

                                            x.onValidate <-
                                                Some (
                                                    fst
                                                    >> DateTime.TryParse
                                                    >> function
                                                        | true, value -> value
                                                        | _ -> DateTime.Parse "00:00"
                                                    >> FlukeTime.FromDateTime
                                                    >> Some
                                                )

                                            x.inputFormat <- Some Input.InputFormat.Time
                                    Props =
                                        fun x ->
                                            x.placeholder <- "00:00"

                                            x.onChange <-
                                                (fun (e: KeyboardEvent) ->
                                                    promise {
                                                        e.Value
                                                        |> DateTime.Parse
                                                        |> FlukeTime.FromDateTime
                                                        |> Some
                                                        |> tempMissedAfter.SetValue
                                                    })
                                |}
                        | None -> nothing
                    ]
            ]

    [<ReactComponent>]
    let PendingAfterSelector taskId =
        let dayStart = Store.useValue Atoms.User.dayStart

        let tempPendingAfter =
            Store.Hooks.useTempAtom
                (Some (Store.InputAtom (Store.AtomReference.Atom (Atoms.Task.pendingAfter taskId))))
                (Some (Store.InputScope.Temp Gun.defaultSerializer))

        UI.box
            (fun x -> x.display <- "inline")
            [
                InputLabel.InputLabel
                    {|
                        Hint = None
                        HintTitle = None
                        Label = str "Pending After"
                        Props = fun x -> x.marginBottom <- "5px"
                    |}

                UI.stack
                    (fun x ->
                        x.direction <- "row"
                        x.spacing <- "15px")
                    [
                        Checkbox.Checkbox
                            (if tempPendingAfter.Value.IsNone then Some "Enable" else None)
                            (fun x ->
                                x.isChecked <- tempPendingAfter.Value.IsSome
                                x.alignSelf <- "center"

                                x.onChange <-
                                    fun _ ->
                                        promise {
                                            tempPendingAfter.SetValue (
                                                if tempPendingAfter.Value.IsSome then None else (Some dayStart)
                                            )
                                        })

                        match tempPendingAfter.Value with
                        | Some pendingAfter ->
                            Input.Input
                                {|
                                    CustomProps =
                                        fun x ->
                                            x.fixedValue <- Some pendingAfter
                                            x.onFormat <- Some FlukeTime.Stringify

                                            x.onValidate <-
                                                Some (
                                                    fst
                                                    >> DateTime.TryParse
                                                    >> function
                                                        | true, value -> value
                                                        | _ -> DateTime.Parse "00:00"
                                                    >> FlukeTime.FromDateTime
                                                    >> Some
                                                )

                                            x.inputFormat <- Some Input.InputFormat.Time
                                    Props =
                                        fun x ->
                                            x.placeholder <- "00:00"

                                            x.onChange <-
                                                (fun (e: KeyboardEvent) ->
                                                    promise {
                                                        e.Value
                                                        |> DateTime.Parse
                                                        |> FlukeTime.FromDateTime
                                                        |> Some
                                                        |> tempPendingAfter.SetValue
                                                    })
                                |}
                        | None -> nothing
                    ]
            ]

    [<ReactComponent>]
    let DurationSelector taskId =
        let tempDuration =
            Store.Hooks.useTempAtom
                (Some (Store.InputAtom (Store.AtomReference.Atom (Atoms.Task.duration taskId))))
                (Some (Store.InputScope.Temp Gun.defaultSerializer))

        UI.box
            (fun x -> x.display <- "inline")
            [
                InputLabel.InputLabel
                    {|
                        Hint = None
                        HintTitle = None
                        Label = str "Duration (minutes)"
                        Props = fun x -> x.marginBottom <- "5px"
                    |}

                UI.stack
                    (fun x ->
                        x.direction <- "row"
                        x.spacing <- "15px")
                    [
                        Checkbox.Checkbox
                            (if tempDuration.Value.IsNone then Some "Enable" else None)
                            (fun x ->
                                x.isChecked <- tempDuration.Value.IsSome
                                x.alignSelf <- "center"

                                x.onChange <-
                                    fun _ ->
                                        promise {
                                            tempDuration.SetValue (
                                                if tempDuration.Value.IsSome then None else (Some (Minute 1))
                                            )
                                        })

                        match tempDuration.Value with
                        | Some duration ->
                            Input.Input
                                {|
                                    CustomProps =
                                        fun x ->
                                            x.fixedValue <- Some duration
                                            x.onFormat <- Some (Minute.Value >> string)

                                            //                                            x.atom <-
//                                                Some (
//                                                    Store.InputAtom (
//                                                        Store.AtomReference.Atom (Atoms.Task.duration taskId)
//                                                    )
//                                                )
//
//                                            x.inputScope <- Some (Store.InputScope.ReadWrite Gun.defaultSerializer)

                                            x.onValidate <-
                                                Some (
                                                    fst
                                                    >> String.parseIntMin 1
                                                    >> Option.defaultValue 1
                                                    >> Minute
                                                    >> Some
                                                )

                                            x.inputFormat <- Some Input.InputFormat.Number
                                    Props =
                                        fun x ->
                                            x.onChange <-
                                                (fun (e: KeyboardEvent) ->
                                                    promise {
                                                        e.Value
                                                        |> int
                                                        |> Minute
                                                        |> Some
                                                        |> tempDuration.SetValue
                                                    })
                                |}
                        | None -> nothing
                    ]
            ]

    [<ReactComponent>]
    let PrioritySelector taskId =
        let tempPriority =
            Store.Hooks.useTempAtom
                (Some (Store.InputAtom (Store.AtomReference.Atom (Atoms.Task.priority taskId))))
                (Some (Store.InputScope.Temp Gun.defaultSerializer))

        let priorityNumber =
            React.useMemo (
                (fun () ->
                    match tempPriority.Value with
                    | Some priority ->
                        let priorityNumber = (priority |> Priority.toTag) + 1
                        Some priorityNumber
                    | None -> None),
                [|
                    box tempPriority.Value
                |]
            )

        UI.box
            (fun x -> x.display <- "inline")
            [
                InputLabel.InputLabel
                    {|
                        Hint = None
                        HintTitle = None
                        Label = str "Priority"
                        Props = fun x -> x.marginBottom <- "5px"
                    |}

                UI.stack
                    (fun x ->
                        x.direction <- "row"
                        x.spacing <- "15px")
                    [
                        Checkbox.Checkbox
                            (if priorityNumber.IsNone then Some "Enable" else None)
                            (fun x ->
                                x.isChecked <- priorityNumber.IsSome

                                x.onChange <-
                                    fun _ ->
                                        promise {
                                            tempPriority.SetValue (
                                                if priorityNumber.IsSome then None else (Some Medium5)
                                            )
                                        })

                        match priorityNumber with
                        | Some priorityNumber ->
                            UI.slider
                                (fun x ->
                                    x.min <- 1
                                    x.max <- 10
                                    x.value <- priorityNumber

                                    x.onChange <-
                                        fun x ->
                                            promise {
                                                tempPriority.SetValue (
                                                    match x with
                                                    | 1 -> Some Low1
                                                    | 2 -> Some Low2
                                                    | 3 -> Some Low3
                                                    | 4 -> Some Medium4
                                                    | 5 -> Some Medium5
                                                    | 6 -> Some Medium6
                                                    | 7 -> Some High7
                                                    | 8 -> Some High8
                                                    | 9 -> Some High9
                                                    | 10 -> Some Critical10
                                                    | _ -> None
                                                )
                                            })
                                [
                                    let bgColor =
                                        if priorityNumber <= 3 then "#68d638"
                                        elif priorityNumber <= 6 then "#f5ec13"
                                        elif priorityNumber <= 9 then "#e44c07"
                                        else "#a13c0e"

                                    UI.sliderTrack
                                        (fun x -> x.backgroundColor <- $"{bgColor}55")
                                        [
                                            UI.sliderFilledTrack (fun x -> x.backgroundColor <- bgColor) []
                                        ]

                                    UI.sliderThumb (fun _ -> ()) []
                                ]

                            UI.box
                                (fun _ -> ())
                                [
                                    str (string priorityNumber)
                                ]
                        | None -> nothing
                    ]
            ]

    let useDeleteTask () =
        Store.useCallback (
            (fun getter _ taskId ->
                promise {
                    do! Store.deleteRoot getter (Atoms.Task.databaseId taskId)
                    return true
                }),
            [||]
        )

    [<ReactComponent>]
    let AddTaskButton () =
        let navigate = Navigate.useNavigate ()
        let taskUIFlag = Store.useValue (Atoms.User.uiFlag UIFlagType.Task)

        let databaseId =
            React.useMemo (
                (fun () ->
                    match taskUIFlag with
                    | UIFlag.Task (databaseId, _) -> databaseId
                    | _ -> Database.Default.Id),
                [|
                    box taskUIFlag
                |]
            )

        Tooltip.wrap
            (str "Add Task")
            [
                TransparentIconButton.TransparentIconButton
                    {|
                        Props =
                            fun x ->
                                UI.setTestId x "Add Task"
                                x.icon <- Icons.fi.FiPlus |> Icons.render
                                x.fontSize <- "17px"

                                x.onClick <-
                                    fun _ ->
                                        navigate (
                                            Navigate.DockPosition.Right,
                                            Some DockType.Task,
                                            UIFlagType.Task,
                                            UIFlag.Task (databaseId, Task.Default.Id)
                                        )
                    |}
            ]

    [<ReactComponent>]
    let rec TaskForm (taskId: TaskId) (onSave: Task -> JS.Promise<unit>) =
        let toast = UI.useToast ()
        let debug = Store.useValue Atoms.debug
        let startSession = useStartSession ()
        let deleteTask = useDeleteTask ()
        let sessions, setSessions = Store.useState (Atoms.Task.sessions taskId)
        let isReadWrite = Store.useValue (Selectors.Task.isReadWrite taskId)
        let taskUIFlag, setTaskUIFlag = Store.useState (Atoms.User.uiFlag UIFlagType.Task)
        let attachmentIdSet = Store.useValue (Atoms.Task.attachmentIdSet taskId)
        let cellAttachmentIdMap = Store.useValue (Atoms.Task.cellAttachmentIdMap taskId)
        let statusMap = Store.useValue (Atoms.Task.statusMap taskId)

        let taskDatabaseId, attachmentIdList =
            React.useMemo (
                (fun () ->
                    let taskDatabaseId =
                        match taskUIFlag with
                        | UIFlag.Task (databaseId, taskId') when taskId' = taskId -> databaseId
                        | _ -> Database.Default.Id

                    taskDatabaseId, (attachmentIdSet |> Set.toList)),
                [|
                    box taskUIFlag
                    box taskId
                    box attachmentIdSet
                |]
            )

        let onAttachmentAdd =
            Store.useCallback (
                (fun _ setter attachmentId ->
                    promise { Store.change setter (Atoms.Task.attachmentIdSet taskId) (Set.add attachmentId) }),
                [|
                    box taskId
                |]
            )


        let onAttachmentDelete =
            Store.useCallback (
                (fun getter setter attachmentId ->
                    promise {
                        Store.change setter (Atoms.Task.attachmentIdSet taskId) (Set.remove attachmentId)

                        do! Store.deleteRoot getter (Atoms.Attachment.attachment attachmentId)
                        return true
                    }),
                [|
                    box taskId
                |]
            )

        let tempInformation =
            Store.Hooks.useTempAtom
                (Some (Store.InputAtom (Store.AtomReference.Atom (Atoms.Task.information taskId))))
                (Some (Store.InputScope.Temp Gun.defaultSerializer))

        let onSave =
            Store.useCallback (
                (fun getter setter _ ->
                    promise {
                        let taskName = Store.getTempValue getter (Atoms.Task.name taskId)
                        let taskInformation = Store.getTempValue getter (Atoms.Task.information taskId)
                        let taskScheduling = Store.getTempValue getter (Atoms.Task.scheduling taskId)
                        let taskPriority = Store.getTempValue getter (Atoms.Task.priority taskId)
                        let taskDuration = Store.getTempValue getter (Atoms.Task.duration taskId)
                        let taskMissedAfter = Store.getTempValue getter (Atoms.Task.missedAfter taskId)
                        let taskPendingAfter = Store.getTempValue getter (Atoms.Task.pendingAfter taskId)

                        if taskDatabaseId = Database.Default.Id then
                            toast (fun x -> x.description <- "Invalid database")
                        elif (match taskName |> TaskName.Value with
                              | String.InvalidString -> true
                              | _ -> false) then
                            toast (fun x -> x.description <- "Invalid name")
                        elif (match taskInformation
                                    |> Information.Name
                                    |> InformationName.Value with
                              | String.InvalidString -> true
                              | _ -> false) then
                            toast (fun x -> x.description <- "Invalid information")
                        else
                            //
//                            let eventId = Atoms.Events.newEventId ()
//                            let event = Atoms.Events.Event.AddTask (eventId, name)
//                            setter.set (Atoms.Events.events eventId, event)
//                            printfn $"event {event}"

                            let! task =
                                if taskId = Task.Default.Id then
                                    { Task.Default with
                                        Id = TaskId.NewId ()
                                        Name = taskName
                                        Information = taskInformation
                                        Scheduling = taskScheduling
                                        Priority = taskPriority
                                        Duration = taskDuration
                                        MissedAfter = taskMissedAfter
                                        PendingAfter = taskPendingAfter
                                    }
                                    |> Promise.lift
                                else
                                    promise {
                                        let task = Store.value getter (Selectors.Task.task taskId)

                                        return
                                            { task with
                                                Name = taskName
                                                Information = taskInformation
                                                Scheduling = taskScheduling
                                                Priority = taskPriority
                                                Duration = taskDuration
                                                MissedAfter = taskMissedAfter
                                                PendingAfter = taskPendingAfter
                                            }
                                    }

                            Store.resetTempValue setter (Atoms.Task.name taskId)
                            Store.resetTempValue setter (Atoms.Task.information taskId)
                            Store.resetTempValue setter (Atoms.Task.scheduling taskId)
                            Store.resetTempValue setter (Atoms.Task.priority taskId)
                            Store.resetTempValue setter (Atoms.Task.duration taskId)
                            Store.resetTempValue setter (Atoms.Task.missedAfter taskId)
                            Store.resetTempValue setter (Atoms.Task.pendingAfter taskId)

                            Store.set setter (Atoms.User.uiFlag UIFlagType.Task) UIFlag.None

                            do! onSave task
                    }),
                [|
                    box taskId
                    box onSave
                    box toast
                    box taskDatabaseId
                |]
            )

        let deleteSession =
            Store.useCallback (
                (fun _ _ start ->
                    promise {
                        let index =
                            sessions
                            |> List.findIndex (fun (Session start') -> start' = start)

                        setSessions (sessions |> List.removeAt index)

                        return true
                    }),
                [|
                    box sessions
                    box setSessions
                |]
            )

        Accordion.Accordion
            {|
                Props = fun x -> x.flex <- "1"
                Atom = Atoms.User.accordionHiddenFlag AccordionType.TaskForm
                Items =
                    [
                        if taskId <> Task.Default.Id then
                            str "Info",
                            (UI.stack
                                (fun x -> x.spacing <- "15px")
                                [
                                    UI.box
                                        (fun _ -> ())
                                        [
                                            str $"Cell Status Count: {statusMap |> Map.count}"
                                        ]
                                    UI.box
                                        (fun _ -> ())
                                        [
                                            str
                                                $"Cell Attachment Count: {cellAttachmentIdMap
                                                                          |> Map.values
                                                                          |> Seq.map Set.count
                                                                          |> Seq.sum}"
                                        ]
                                ])

                        (UI.box
                            (fun _ -> ())
                            [
                                str $"""{if taskId = Task.Default.Id then "Add" else "Edit"} Task"""

                                if taskId <> Task.Default.Id then
                                    Menu.Menu
                                        {|
                                            Tooltip = ""
                                            Trigger =
                                                Menu.FakeMenuButton
                                                    InputLabelIconButton.InputLabelIconButton
                                                    (fun x ->
                                                        x.icon <- Icons.bs.BsThreeDots |> Icons.render
                                                        x.fontSize <- "11px"
                                                        x.height <- "15px"
                                                        x.color <- "whiteAlpha.700"
                                                        x.display <- if isReadWrite then null else "none"
                                                        x.marginTop <- "-3px"
                                                        x.marginLeft <- "6px")
                                            Body =
                                                [
                                                    Popover.MenuItemConfirmPopover
                                                        Icons.bi.BiTrash
                                                        "Delete Task"
                                                        (fun () -> deleteTask taskId)
                                                ]
                                            MenuListProps = fun _ -> ()
                                        |}

                            ]),
                        (UI.stack
                            (fun x -> x.spacing <- "15px")
                            [
                                if not debug then
                                    nothing
                                else
                                    UI.box
                                        (fun _ -> ())
                                        [
                                            str $"{taskId}"
                                        ]

                                DatabaseSelector.DatabaseSelector
                                    taskDatabaseId
                                    (fun databaseId -> setTaskUIFlag (UIFlag.Task (databaseId, taskId)))

                                InformationSelector.InformationSelector
                                    {|
                                        DisableResource = true
                                        SelectionType = InformationSelector.InformationSelectionType.Information
                                        Information = Some tempInformation.Value
                                        OnSelect = tempInformation.SetValue
                                    |}

                                Input.Input
                                    {|
                                        CustomProps =
                                            fun x ->
                                                x.atom <-
                                                    Some (
                                                        Store.InputAtom (
                                                            Store.AtomReference.Atom (Atoms.Task.name taskId)
                                                        )
                                                    )

                                                x.inputScope <- Some (Store.InputScope.Temp Gun.defaultSerializer)

                                                x.onEnterPress <- Some onSave
                                                x.onFormat <- Some (fun (TaskName name) -> name)
                                                x.onValidate <- Some (fst >> TaskName >> Some)
                                        Props =
                                            fun x ->
                                                x.autoFocus <- true
                                                x.label <- str "Name"

                                                x.placeholder <- $"""new-task-{DateTime.Now.Format "yyyy-MM-dd"}"""
                                    |}

                                SchedulingSelector.SchedulingSelector taskId

                                PrioritySelector taskId

                                DurationSelector taskId

                                PendingAfterSelector taskId

                                MissedAfterSelector taskId

                                Button.Button
                                    {|
                                        Hint = None
                                        Icon = Some (Icons.fi.FiSave |> Icons.render, Button.IconPosition.Left)
                                        Props = fun x -> x.onClick <- onSave
                                        Children =
                                            [
                                                str "Save"
                                            ]
                                    |}
                            ])

                        if taskId <> Task.Default.Id then
                            (UI.box
                                (fun _ -> ())
                                [
                                    str "Sessions"

                                    Menu.Menu
                                        {|
                                            Tooltip = ""
                                            Trigger =
                                                Menu.FakeMenuButton
                                                    InputLabelIconButton.InputLabelIconButton
                                                    (fun x ->
                                                        x.icon <- Icons.bs.BsThreeDots |> Icons.render
                                                        x.fontSize <- "11px"
                                                        x.height <- "15px"
                                                        x.color <- "whiteAlpha.700"
                                                        x.display <- if isReadWrite then null else "none"
                                                        x.marginTop <- "-3px"
                                                        x.marginLeft <- "6px")
                                            Body =
                                                [
                                                    MenuItem.MenuItem
                                                        Icons.gi.GiHourglass
                                                        "Start Session"
                                                        (Some (fun () -> startSession taskId))
                                                        (fun _ -> ())
                                                ]
                                            MenuListProps = fun _ -> ()
                                        |}

                                ]),
                            (match sessions with
                             | [] ->
                                 UI.box
                                     (fun _ -> ())
                                     [
                                         str "No sessions found"
                                     ]
                             | sessions ->
                                 UI.stack
                                     (fun _ -> ())
                                     [
                                         yield!
                                             sessions
                                             |> List.map
                                                 (fun (Session start) ->
                                                     UI.flex
                                                         (fun x ->
                                                             x.key <- $"session-{start |> FlukeDateTime.Stringify}")
                                                         [
                                                             UI.box
                                                                 (fun _ -> ())
                                                                 [
                                                                     str (start |> FlukeDateTime.Stringify)

                                                                     Menu.Menu
                                                                         {|
                                                                             Tooltip = ""
                                                                             Trigger =
                                                                                 InputLabelIconButton.InputLabelIconButton
                                                                                     (fun x ->
                                                                                         x.``as`` <- UI.react.MenuButton

                                                                                         x.icon <-
                                                                                             Icons.bs.BsThreeDots
                                                                                             |> Icons.render

                                                                                         x.fontSize <- "11px"
                                                                                         x.height <- "15px"
                                                                                         x.color <- "whiteAlpha.700"
                                                                                         x.marginTop <- "-1px"
                                                                                         x.marginLeft <- "6px")
                                                                             Body =
                                                                                 [
                                                                                     Popover.MenuItemConfirmPopover
                                                                                         Icons.bi.BiTrash
                                                                                         "Delete Session"
                                                                                         (fun () -> deleteSession start)
                                                                                 ]
                                                                             MenuListProps = fun _ -> ()
                                                                         |}
                                                                 ]
                                                         ])
                                     ])

                            str "Attachments",
                            (UI.stack
                                (fun x ->
                                    x.spacing <- "10px"
                                    x.flex <- "1")
                                [
                                    AttachmentPanel.AttachmentPanel
                                        AddAttachmentInput.AttachmentPanelType.Task
                                        (Some onAttachmentAdd)
                                        onAttachmentDelete
                                        attachmentIdList
                                ])
                    ]
            |}

    [<ReactComponent>]
    let TaskFormWrapper () =
        let hydrateTaskState = Hydrate.useHydrateTaskState ()
        let hydrateTask = Hydrate.useHydrateTask ()
        let archive = Store.useValue Atoms.User.archive
        let selectedTaskIdListByArchive = Store.useValue Selectors.Session.selectedTaskIdListByArchive
        let setRightDock = Store.useSetState Atoms.User.rightDock
        let taskUIFlag = Store.useValue (Atoms.User.uiFlag UIFlagType.Task)

        let taskDatabaseId =
            match taskUIFlag with
            | UIFlag.Task (databaseId, _) -> databaseId
            | _ -> Database.Default.Id

        let taskId =
            React.useMemo (
                (fun () ->
                    match taskUIFlag with
                    | UIFlag.Task (_, taskId) when
                        selectedTaskIdListByArchive
                        |> List.contains taskId
                        ->
                        taskId
                    | _ -> Task.Default.Id),
                [|
                    box taskUIFlag
                    box selectedTaskIdListByArchive
                |]
            )

        TaskForm
            taskId
            (fun task ->
                promise {
                    if task.Id <> taskId then
                        let taskState =
                            {
                                Task = task
                                Archived = archive |> Option.defaultValue false
                                SortList = []
                                SessionList = []
                                AttachmentStateList = []
                                CellStateMap = Map.empty
                            }

                        do! hydrateTaskState (Store.AtomScope.Current, taskDatabaseId, taskState)
                    else
                        do! hydrateTask (Store.AtomScope.Current, taskDatabaseId, task)

                    setRightDock None
                })
