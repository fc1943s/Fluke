namespace Fluke.UI.Frontend.Components

open System
open Browser
open Fable.Core
open Fluke.UI.Frontend
open Browser.Types
open FSharpPlus
open Fluke.Shared
open Fable.React
open Fable.React.Props
open Fable.DateFunctions
open Fulma
open Fulma.Extensions.Wikiki
open Suigetsu.UI.Frontend.React
open Suigetsu.Core
open Feliz
open Feliz.Recoil
open Feliz.Bulma


module SuigetsuTemp =
    module CustomHooks =
        let useWindowSize () =
            let getWindowSize () =
                {| Width = window.innerWidth
                   Height = window.innerHeight |}
            let size, setSize = React.useState (getWindowSize ())

            React.useLayoutEffect (fun () ->
                let updateSize (_event: Event) =
                    setSize (getWindowSize ())

                window.addEventListener ("resize", updateSize)

                { new IDisposable with
                    member _.Dispose () =
                        window.removeEventListener ("resize", updateSize)
                }
            )
            size

module PageLoaderComponent =
    let render = React.memo (fun () ->
        PageLoader.pageLoader [ PageLoader.Color IsDark
                                PageLoader.IsActive true ][]
    )

module NavBarComponent =
    open Model
    let render = React.memo (fun () ->
        let view, setView = Recoil.useState Recoil.Atoms.view
        let activeSessions = Recoil.useValue Recoil.Atoms.activeSessions

        Ext.useEventListener "keydown" (fun (e: KeyboardEvent) ->
            match e.ctrlKey, e.shiftKey, e.key with
            | _, true, "C" -> setView View.Calendar
            | _, true, "G" -> setView View.Groups
            | _, true, "T" -> setView View.Tasks
            | _, true, "W" -> setView View.Week
            | _            -> ()
        )

//        Bulma.navbar [
//            prop.children [
//            ]
//
//        ]
        Navbar.navbar [ Navbar.Color IsBlack
                        Navbar.Props [ Style [ Height 36
                                               MinHeight 36
                                               Display DisplayOptions.Flex
                                               JustifyContent "space-around" ]]][

            let checkbox newView text =
                Bulma.navbarItem.div [
                    prop.className "field"
                    prop.onClick (fun _ -> setView newView)
                    prop.style [
                        style.marginBottom 0
                        style.alignSelf.center
                    ]
                    prop.children [
                        Checkbox.input [ CustomClass "switch is-small is-dark"
                                         Props [ Checked (view = newView)
                                                 OnChange (fun _ -> ()) ]]

                        Checkbox.checkbox [][
                            str text
                        ]
                    ]
                ]

            checkbox View.Calendar "calendar view"
            checkbox View.Groups "groups view"
            checkbox View.Tasks "tasks view"
            checkbox View.Week "week view"

            Bulma.navbarItem.div [
                activeSessions
                |> List.map (fun (ActiveSession (task, duration)) ->
                    let sessionType, color, duration, left =
                        let left = TempData.sessionLength - duration
                        match duration < TempData.sessionLength with
                        | true  -> "Session", "#7cca7c", duration, left
                        | false -> "Break",   "#ca7c7c", -left,    TempData.sessionBreakLength + left

                    Html.span [
                        prop.style [
                            style.color color
                        ]
                        prop.children [
                            sprintf "%s: Task[ %s ]; Duration[ %.1f ]; Left[ %.1f ]" sessionType task.Name duration left
                            |> str
                        ]
                    ]
                )
                |> List.intersperse (br [])
                |> function
                    | [] -> str "No active session"
                    | list -> ofList list
            ]

        ]
    )

module TooltipPopupComponent =
    open Model

    let render = React.memo (fun (input: {| Comments: Comment list |}) ->
        Html.div [
            prop.className Css.tooltipPopup
            prop.children [
                input.Comments
                |> List.map (fun (Comment (_user, comment)) -> comment.Trim ())
                |> List.map ((+) Environment.NewLine)
                |> String.concat (Environment.NewLine + Environment.NewLine)
                |> fun text ->
                    ReactBindings.React.createElement
                        (Ext.reactMarkdown, {| source = text |}, [])
            ]
        ]
    )

module ApplicationComponent =
    open Model

    module Grid =
        let emptyDiv =
            Html.div [
                prop.dangerouslySetInnerHTML "&nbsp;"
            ]

        let paddingLeftLevel level =
            style.paddingLeft (20 * level)

        let header = React.memo (fun () ->
            let dateSequence = Recoil.useValue Recoil.Selectors.dateSequence
            let dateMap = Recoil.useValue Recoil.Selectors.dateMap

            Html.div [
                // Month row
                Html.div [
                    prop.style [
                        style.display.flex
                    ]
                    prop.children [
                        yield! dateSequence
                        |> List.groupBy (fun date -> date.Month)
                        |> List.map (fun (_, dates) -> dates.Head, dates.Length)
                        |> List.map (fun (firstDay, days) ->
                            Html.span [
                                prop.style [
                                    style.textAlign.center
                                    style.width (17 * days)

                                ]
                                prop.children [
                                    str (firstDay.DateTime.Format "MMM")
                                ]
                            ]
                        )
                    ]
                ]

                // Day of Week row
                Html.div [
                    prop.style [
                        style.display.flex
                    ]
                    prop.children [
                        yield! dateSequence
                        |> List.map (fun date ->
                            Html.span [
                                prop.classes [
                                    if dateMap.[date].IsToday then Css.todayHeader
                                    if dateMap.[date].IsSelected then Css.selectionHighlight
                                ]
                                prop.style [
                                    style.width 17
                                    style.textAlign.center
                                    match Functions.getCellSeparatorBorderLeft date with
                                    | Some borderLeft -> borderLeft
                                    | None -> ()
                                ]
                                prop.children [
                                    date.DateTime.Format "EEEEEE"
                                    |> String.toLower
                                    |> str
                                ]
                            ]
                        )
                    ]
                ]

                // Day row
                Html.div [
                    prop.style [
                        style.display.flex
                    ]
                    prop.children [
                        yield! dateSequence
                        |> List.map (fun date ->
                            Html.span [
                                prop.classes [
                                    if dateMap.[date].IsToday then Css.todayHeader
                                    if dateMap.[date].IsSelected then Css.selectionHighlight
                                ]
                                prop.style [
                                    style.width 17
                                    style.textAlign.center
                                    match Functions.getCellSeparatorBorderLeft date with
                                    | Some borderLeft -> borderLeft
                                    | None -> ()
                                ]
                                prop.children [
                                    str (date.Day.ToString "D2")
                                ]
                            ]
                        )
                    ]
                ]
            ]
        )

        let taskName = React.memo (fun (input: {| Level: int
                                                  TaskId: Recoil.Atoms.RecoilTask.TaskId |}) ->
            let selection = Recoil.useValue Recoil.Selectors.selection

            let task = Recoil.useValue (Recoil.Atoms.RecoilTask.taskFamily input.TaskId)

            let taskName = Recoil.useValue task.Name
            let taskComments = Recoil.useValue task.Comments


            let isSelected =
                selection
                |> Map.tryFind input.TaskId
                |> Option.defaultValue Set.empty
                |> Set.isEmpty
                |> not

            Html.div [
                prop.classes [
                    if not taskComments.IsEmpty then Css.tooltipContainer
                ]
                prop.style [
                    style.height 17
                ]
                prop.children [
                    Html.div [
                        prop.classes [
                            if isSelected then Css.selectionHighlight
                        ]
                        prop.style [
                            style.overflow.hidden
                            style.whitespace.nowrap
                            style.textOverflow.ellipsis
                            paddingLeftLevel input.Level
                        ]
                        prop.children [
                            str taskName
                        ]
                    ]
                    if not taskComments.IsEmpty then
                        TooltipPopupComponent.render {| Comments = taskComments |}
                ]
            ]
        )

        let cell = React.memo (fun (input: {| TaskId: Recoil.Atoms.RecoilTask.TaskId
                                              Date: FlukeDate |}) ->
            let isToday = Recoil.useValue (Recoil.Selectors.isTodayFamily input.Date)

            let cellId = Recoil.Atoms.RecoilCell.cellId input.TaskId input.Date
            let cell = Recoil.useValue (Recoil.Atoms.RecoilCell.cellFamily cellId)

            let comments = Recoil.useValue cell.Comments
            let sessions = Recoil.useValue cell.Sessions
            let status = Recoil.useValue cell.Status
            let selected, setSelected = Recoil.useState (Recoil.Selectors.RecoilCell.selected cellId)


//            printfn "R: %A\t%A\t%A" cellId taskId date

//            let status = Recoil.useValue cell.Status

    //        let selected, setSelected = false, fun _ -> ()

            let events = {|
                OnCellClick = fun () ->
                    setSelected (not selected)
            |}

            Html.div [
                prop.classes [
                    status.CellClass
                    if not comments.IsEmpty then
                        Css.tooltipContainer
                    if selected then
                        Css.cellSelected
                    if isToday then
                        Css.cellToday
                ]
                prop.children [
                    Html.div [
                        prop.style [
                            match Functions.getCellSeparatorBorderLeft input.Date with
                            | Some borderLeft -> borderLeft
                            | None -> ()
                        ]
                        prop.onClick (fun (_event: MouseEvent) ->
                            events.OnCellClick ()
                        )
                        prop.children [
                            match sessions.Length with
            //                | x -> str (string x)
                            | x when x > 0 -> str (string x)
                            | _ -> ()
                        ]
                    ]

                    if not comments.IsEmpty then
                        TooltipPopupComponent.render {| Comments = comments |}
                ]
            ]
        )

        let cells = React.memo (fun () ->
            let dateSequence = Recoil.useValue Recoil.Selectors.dateSequence
            let taskIdList = Recoil.useValue Recoil.Atoms.taskIdList

            Html.div [
                prop.className Css.laneContainer
                prop.children [
                    yield! taskIdList
                    |> List.map (fun taskId ->
                        Html.div [
                            yield! dateSequence
                            |> List.map (fun date ->
                                cell {| TaskId = taskId; Date = date |}
                            )
                        ]
                    )
                ]
            ]
        )

    module CalendarViewComponent =
        let render = React.memo (fun () ->
            let taskList = Recoil.useValue Recoil.Selectors.taskList

            Html.div [
                prop.style [
                    style.display.flex
                ]
                prop.children [
                    // Column: Left
                    Html.div [
                        // Top Padding
                        Html.div [
                            yield! Grid.emptyDiv |> List.replicate 3
                        ]
                        Html.div [
                            prop.style [
                                style.display.flex
                            ]
                            prop.children [
                                // Column: Information Type
                                Html.div [
                                    prop.style [
                                        style.paddingRight 10
                                    ]
                                    prop.children [
                                        yield! taskList
                                        |> List.map (fun task ->
                                            Html.div [
                                                prop.classes [
                                                    if not task.InformationComments.IsEmpty then
                                                        Css.blueIndicator
                                                        Css.tooltipContainer
                                                ]
                                                prop.style [
                                                    style.padding 0
                                                    style.height 17
                                                    style.color task.Information.Color
                                                    style.whitespace.nowrap
                                                ]
                                                prop.children [
                                                    str task.Information.Name
                                                    if not task.InformationComments.IsEmpty then
                                                        TooltipPopupComponent.render {| Comments = task.InformationComments |}
                                                ]
                                            ]
                                        )
                                    ]
                                ]
                                // Column: Task Name
                                Html.div [
                                    prop.style [
                                        style.width 200
                                    ]
                                    prop.children [
                                        yield! taskList
                                        |> List.map (fun task ->
                                            Grid.taskName {| Level = 0; TaskId = task.Id |}
                                        )
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        Grid.header ()
                        Grid.cells ()
                    ]
                ]
            ]
        )

    module GroupsViewComponent =
        let render = React.memo (fun () ->
            let taskList = Recoil.useValue Recoil.Selectors.taskList

            let groupMap =
                taskList
                |> List.map (fun x -> x.Information, x)
                |> Map.ofList

            let groups =
                taskList
                |> List.groupBy (fun group -> group.Information)
                |> List.groupBy (fun (info, _) -> info.KindName)

            Html.div [
                prop.style [
                    style.display.flex
                ]
                prop.children [
                    // Column: Left
                    Html.div [
                        // Top Padding
                        Html.div [
                            yield! Grid.emptyDiv |> List.replicate 3
                        ]
                        Html.div [
                            yield! groups
                            |> List.map (fun (informationKindName, taskGroups) ->
                                Html.div [
                                    // Information Type
                                    Html.div [
                                        prop.style [
                                            style.color "#444"
                                        ]
                                        prop.children [
                                            str informationKindName
                                        ]
                                    ]
                                    Html.div [
                                        yield! taskGroups
                                        |> List.map (fun (information, group) ->
                                            let informationComments = groupMap.[information].InformationComments
                                            Html.div [
                                                // Information
                                                Html.div [
                                                    prop.classes [
                                                        if informationComments.IsEmpty then
                                                            Css.blueIndicator
                                                            Css.tooltipContainer
                                                    ]
                                                    prop.style [
                                                        style.color "#444"
                                                        Grid.paddingLeftLevel 1
                                                    ]
                                                    prop.children [
                                                        str information.Name
                                                        if not informationComments.IsEmpty then
                                                            TooltipPopupComponent.render {| Comments = informationComments |}
                                                    ]
                                                ]

                                                // Task Name
                                                Html.div [
                                                    prop.style [
                                                        style.width 500
                                                    ]
                                                    prop.children [
                                                        yield! group
                                                        |> List.map (fun groupTask ->
                                                            Grid.taskName
                                                                {| Level = 2; TaskId = groupTask.Id |}
                                                        )
                                                    ]
                                                ]
                                            ]
                                        )
                                    ]
                                ]
                            )
                        ]
                    ]
                    // Column: Grid
                    Html.div [
                        Grid.header ()
                        Html.div [
                            yield! groups
                            |> List.map (fun (_, taskGroups) ->
                                Html.div [
                                    Grid.emptyDiv
                                    Html.div [
                                        yield! taskGroups
                                        |> List.map (fun (_, _tasks) ->
                                            Html.div [
                                                Grid.emptyDiv
                                                Grid.cells ()
                                            ]
                                        )
                                    ]
                                ]
                            )
                        ]
                    ]
                ]
            ]
        )

    module TasksViewComponent =
        let render = React.memo (fun () ->
            let taskList = Recoil.useValue Recoil.Selectors.taskList

            Html.div [
                prop.style [
                    style.display.flex
                ]
                prop.children [
                    // Column: Left
                    Html.div [
                        // Top Padding
                        Html.div [
                            yield! Grid.emptyDiv |> List.replicate 3
                        ]
                        Html.div [
                            prop.style [
                                style.display.flex
                            ]
                            prop.children [
                                // Column: Information Type
                                Html.div [
                                    prop.style [
                                        style.paddingRight 10
                                    ]
                                    prop.children [
                                        yield! taskList
                                        |> List.map (fun task ->
                                            Html.div [
                                                prop.classes [
                                                    if task.InformationComments.IsEmpty then
                                                        Css.blueIndicator
                                                        Css.tooltipContainer
                                                ]
                                                prop.style [
                                                    style.padding 0
                                                    style.height 17
                                                    style.color task.Information.Color
                                                    style.whitespace.nowrap
                                                ]
                                                prop.children [
                                                    str task.Information.Name

                                                    if not task.InformationComments.IsEmpty then
                                                        TooltipPopupComponent.render
                                                            {| Comments = task.InformationComments |}
                                                ]
                                            ]
                                        )
                                    ]
                                ]
                                // Column: Priority
                                Html.div [
                                    prop.style [
                                        style.paddingRight 10
                                        style.textAlign.center
                                    ]
                                    prop.children [
                                        yield! taskList
                                        |> List.map (fun task ->
                                            Html.div [
                                                prop.style [
                                                    style.height 17
                                                ]
                                                prop.children [
                                                    task.Priority
                                                    |> ofTaskPriorityValue
                                                    |> string
                                                    |> str
                                                ]
                                            ]
                                        )
                                    ]
                                ]
                                // Column: Task Name
                                Html.div [
                                    prop.style [
                                        style.width 200
                                    ]
                                    prop.children [
                                        yield! taskList
                                        |> List.map (fun task ->
                                            Grid.taskName {| Level = 0; TaskId = task.Id |}
                                        )
                                    ]
                                ]
                            ]
                        ]
                    ]
                    Html.div [
                        Grid.header ()
                        Grid.cells ()
                    ]
                ]
            ]
        )

    module WeekViewComponent =
        let render = React.memo (fun () ->
            nothing
        )

    let render = React.memo (fun () ->

        let view = Recoil.useValue Recoil.Atoms.view
//        let now = Recoil.useValue Recoil.Selectors.now
//        let activeSessions, setActiveSessions = Recoil.useState Recoil.Selectors.activeSessions
//        let selection, setSelection = Recoil.useState Recoil.Selectors.selection
//        let dayStart = Recoil.useValue Recoil.Selectors.dayStart
//        let dateSequence = Recoil.useValue Recoil.Selectors.dateSequence
//        let dings, setDings = Recoil.useState (Recoil.Selectors.dingsFamily now)
//        let ctrlPressed = Recoil.useValue Recoil.Selectors.ctrlPressed
//        let taskStateList = Recoil.useValue Recoil.Selectors.taskStateList
//        let informationList = Recoil.useValue Recoil.Selectors.informationList
//        let taskOrderList = Recoil.useValue Recoil.Selectors.taskOrderList

//        taskStateList
//        |> List.iter (fun taskState ->
//            let setTaskCells = Recoil.useSetState (Recoil.Selectors.taskCellsFamily taskState.Task)
//            setTaskCells cells
//        )

//        let newActiveSessions =
//            lastSessions
//            |> List.map (Tuple2.mapSnd (fun (TaskSession start) -> (now.DateTime - start.DateTime).TotalMinutes))
//            |> List.filter (fun (_, length) -> length < TempData.sessionLength + TempData.sessionBreakLength)
//            |> List.map ActiveSession
//
//        if activeSessions <> newActiveSessions then
//            setActiveSessions newActiveSessions
//
//        newActiveSessions
//        |> List.map (fun (ActiveSession (oldTask, oldDuration)) ->
//            let newSession =
//                activeSessions
//                |> List.tryFind (fun (ActiveSession (task, duration)) ->
//                    task = oldTask && duration = oldDuration + 1.
//                )
//
//            match newSession with
//            | Some (ActiveSession (_, newDuration)) when oldDuration = -1. && newDuration = 0. -> playTick
//            | Some (ActiveSession (_, newDuration)) when newDuration = TempData.sessionLength -> playDing
//            | None when oldDuration = TempData.sessionLength + TempData.sessionBreakLength - 1. -> playDing
//            | _ -> fun () -> ()
//        )
//        |> List.iter (fun x -> x ())

        Html.div [
            match view with
            | View.Calendar -> CalendarViewComponent.render ()
            | View.Groups   -> GroupsViewComponent.render ()
            | View.Tasks    -> TasksViewComponent.render ()
            | View.Week     -> WeekViewComponent.render ()
        ]
    )



    ()

module MainComponent =
    let positionUpdater = React.memo (fun () ->
        let resetPosition = Recoil.useResetState Recoil.Selectors.position

        CustomHooks.useInterval resetPosition (60 * 1000)

        nothing
    )

    let useTimeout fn timeout =
        let savedCallback = Hooks.useRef fn

        Hooks.useEffect (fun () ->
            savedCallback.current <- fn
        , [| fn |])

        Hooks.useEffectDisposable (fun () ->
            let id =
                JS.setTimeout (fun () ->
                    savedCallback.current ()
                ) timeout

            { new IDisposable with
                member _.Dispose () =
                    JS.clearTimeout id }
        , [| timeout |])

    let dataLoader = React.memo (fun () ->
        let updateTree = Recoil.useSetState Recoil.Selectors.treeUpdater

        let updateTree =
            Recoil.useCallback (fun _ ->
                async {
                    updateTree ()
                }
                |> Async.StartImmediate
            )

        updateTree ()

        nothing
    )

    let globalShortcutHandler = React.memo (fun () ->
        let selection, setSelection = Recoil.useState Recoil.Selectors.selection
        let ctrlPressed, setCtrlPressed = Recoil.useState Recoil.Atoms.ctrlPressed

        let keyEvent (e: KeyboardEvent) =
            if e.ctrlKey <> ctrlPressed then
                setCtrlPressed e.ctrlKey

            if e.key = "Escape" && not selection.IsEmpty then
                setSelection Map.empty

        Ext.useEventListener "keydown" keyEvent
        Ext.useEventListener "keyup" keyEvent

        nothing
    )

    let render = React.memo (fun () ->

        Html.div [
            globalShortcutHandler ()
            positionUpdater ()
            dataLoader ()

            React.suspense ([
                NavBarComponent.render ()
                ApplicationComponent.render ()
            ], PageLoaderComponent.render ())
        ]
    )

