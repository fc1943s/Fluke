namespace Fluke.UI.Frontend.Components

open Fluke.Shared.Domain.Model
open Feliz
open Fable.React
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.Hooks
open Fluke.UI.Frontend.State
open Fluke.UI.Frontend.TempUI


type DatabaseNodeMenu () =
    [<ReactComponent>]
    static member DatabaseNodeMenu (databaseId, disabled) =
        let deviceInfo = Store.useValue Selectors.deviceInfo
        let isReadWrite = Store.useValue (Selectors.Database.isReadWrite databaseId)
        let setLeftDock = Store.useSetState Atoms.leftDock
        let setRightDock = Store.useSetState Atoms.rightDock
        let setDatabaseUIFlag = Store.useSetState (Atoms.uiFlag Atoms.UIFlagType.Database)
        let setTaskUIFlag = Store.useSetState (Atoms.uiFlag Atoms.UIFlagType.Task)

        let exportDatabase = Hydrate.useExportDatabase ()

        Menu.Menu
            {|
                Tooltip = ""
                Trigger =
                    InputLabelIconButton.InputLabelIconButton
                        (fun x ->
                            x.``as`` <- Chakra.react.MenuButton
                            x.icon <- Icons.bs.BsThreeDots |> Icons.render
                            x.fontSize <- "11px"
                            x.disabled <- disabled
                            x.marginLeft <- "6px")
                Body =
                    [
                        if isReadWrite then
                            Chakra.menuItem
                                (fun x ->
                                    x.closeOnSelect <- true

                                    x.icon <-
                                        Icons.bs.BsPlus
                                        |> Icons.renderChakra (fun x -> x.fontSize <- "13px")

                                    x.onClick <-
                                        fun e ->
                                            promise {
                                                if deviceInfo.IsMobile then setLeftDock None
                                                setRightDock (Some DockType.Task)
                                                setTaskUIFlag ((databaseId, Task.Default.Id) |> Atoms.UIFlag.Task)
                                                e.preventDefault ()
                                            })
                                [
                                    str "Add Task"
                                ]

                            Chakra.menuItem
                                (fun x ->
                                    x.closeOnSelect <- true

                                    x.icon <-
                                        Icons.bs.BsPen
                                        |> Icons.renderChakra (fun x -> x.fontSize <- "13px")

                                    x.onClick <-
                                        fun e ->
                                            promise {
                                                if deviceInfo.IsMobile then setLeftDock None
                                                setRightDock (Some DockType.Database)
                                                setDatabaseUIFlag (databaseId |> Atoms.UIFlag.Database)
                                                e.preventDefault ()
                                            })
                                [
                                    str "Edit Database"
                                ]

                        Chakra.menuItem
                            (fun x ->
                                x.closeOnSelect <- true

                                x.icon <-
                                    Icons.fi.FiCopy
                                    |> Icons.renderChakra (fun x -> x.fontSize <- "13px")

                                x.isDisabled <- true
                                x.onClick <- fun e -> promise { e.preventDefault () })
                            [
                                str "Clone Database"
                            ]

                        Chakra.menuItem
                            (fun x ->
                                x.closeOnSelect <- true

                                x.icon <-
                                    Icons.bi.BiExport
                                    |> Icons.renderChakra (fun x -> x.fontSize <- "13px")

                                x.onClick <-
                                    fun e ->
                                        e.preventDefault ()
                                        exportDatabase databaseId)
                            [
                                str "Export Database"
                            ]

                        Chakra.menuItem
                            (fun x ->
                                x.closeOnSelect <- true

                                x.icon <-
                                    Icons.bs.BsTrash
                                    |> Icons.renderChakra (fun x -> x.fontSize <- "13px")

                                x.isDisabled <- true

                                x.onClick <- fun e -> promise { e.preventDefault () })
                            [
                                str "Delete Database"
                            ]
                    ]
                MenuListProps = fun _ -> ()
            |}
