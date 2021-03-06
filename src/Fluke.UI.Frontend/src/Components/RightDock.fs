namespace Fluke.UI.Frontend.Components

open Fable.Core
open Feliz
open Fable.React
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Components
open Fluke.UI.Frontend.Bindings
open Fluke.Shared
open Fluke.UI.Frontend.Hooks
open Fluke.UI.Frontend.State


module RightDock =

    [<ReactComponent>]
    let RightDock () =
        let deviceInfo = Store.useValue Selectors.deviceInfo
        let setLeftDock = Store.useSetState Atoms.User.leftDock
        let rightDock = Store.useValue Atoms.User.rightDock

        let rightDockSize, setRightDockSize = Store.useState Atoms.User.rightDockSize

        let items =
            React.useMemo (
                (fun () ->
                    [
                        TempUI.DockType.Database,
                        {|
                            Name = "Database"
                            Icon = Icons.fi.FiDatabase
                            Content = DatabaseForm.DatabaseFormWrapper
                            RightIcons = []
                        |}

                        TempUI.DockType.Information,
                        {|
                            Name = "Information"
                            Icon = Icons.bs.BsListNested
                            Content = InformationForm.InformationFormWrapper
                            RightIcons = []
                        |}

                        TempUI.DockType.Task,
                        {|
                            Name = "Task"
                            Icon = Icons.bs.BsListTask
                            Content = TaskForm.TaskFormWrapper
                            RightIcons =
                                [
                                    DockPanel.DockPanelIcon.Component (TaskForm.AddTaskButton ())
                                ]
                        |}

                        TempUI.DockType.Cell,
                        {|
                            Name = "Cell"
                            Icon = Icons.fa.FaCalendarCheck
                            Content = CellForm.CellFormWrapper
                            RightIcons = []
                        |}

                        TempUI.DockType.Search,
                        {|
                            Name = "Search"
                            Icon = Icons.bs.BsSearch
                            Content = SearchForm.SearchForm
                            RightIcons = []
                        |}
                    ]),
                [||]
            )

        let itemsMap = items |> Map.ofSeq

        UI.flex
            (fun _ -> ())
            [
                match rightDock with
                | None -> nothing
                | Some rightDock ->
                    match itemsMap |> Map.tryFind rightDock with
                    | None -> nothing
                    | Some item ->
                        Resizable.resizable
                            {|
                                size = {| width = $"{rightDockSize}px" |}
                                onResizeStop =
                                    fun _e _direction _ref (d: {| width: int |}) ->
                                        setRightDockSize (rightDockSize + d.width)
                                minWidth = "200px"
                                enable =
                                    {|
                                        top = false
                                        right = false
                                        bottom = false
                                        left = true
                                        topRight = false
                                        bottomRight = false
                                        bottomLeft = false
                                        topLeft = false
                                    |}
                            |}
                            [
                                UI.flex
                                    (fun x ->
                                        x.width <-
                                            unbox (
                                                JS.newObj
                                                    (fun (x: UI.IBreakpoints<string>) ->
                                                        x.``base`` <- "calc(100vw - 52px)"
                                                        x.md <- "auto")
                                            )

                                        x.height <- "100%"
                                        x.borderLeftWidth <- "1px"
                                        x.borderLeftColor <- "gray.16"
                                        x.flex <- "1")
                                    [
                                        DockPanel.DockPanel
                                            {|
                                                Name = item.Name
                                                Icon = item.Icon
                                                RightIcons = item.RightIcons
                                                Atom = Atoms.User.rightDock
                                                children =
                                                    [
                                                        React.suspense (
                                                            [
                                                                item.Content ()
                                                            ],
                                                            LoadingSpinner.LoadingSpinner ()
                                                        )
                                                    ]
                                            |}
                                    ]
                            ]

                UI.box
                    (fun x ->
                        x.width <- "24px"
                        x.position <- "relative"
                        x.margin <- "1px")
                    [
                        UI.stack
                            (fun x ->
                                x.spacing <- "1px"
                                x.direction <- "row"
                                x.left <- "0"
                                x.position <- "absolute"
                                x.transform <- "rotate(90deg) translate(-24px, 0%)"
                                x.transformOrigin <- "0 100%"
                                x.height <- "24px")
                            [
                                yield!
                                    items
                                    |> List.map
                                        (fun (dockType, item) ->
                                            DockButton.DockButton
                                                {|
                                                    DockType = dockType
                                                    Name = item.Name
                                                    Icon = item.Icon
                                                    OnClick =
                                                        fun _ ->
                                                            promise { if deviceInfo.IsMobile then setLeftDock None }
                                                    Atom = Atoms.User.rightDock
                                                    Props = fun _ -> ()
                                                |})
                            ]
                    ]
            ]
