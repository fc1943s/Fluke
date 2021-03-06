namespace Fluke.UI.Frontend.Components

open Fluke.Shared.Domain.Model
open Feliz
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.Hooks
open Fluke.UI.Frontend.State
open Fluke.UI.Frontend.TempUI


module DatabaseNodeMenu =
    [<ReactComponent>]
    let DatabaseNodeMenu databaseId disabled =
        let isReadWrite = Store.useValue (Selectors.Database.isReadWrite databaseId)
        let navigate = Navigate.useNavigate ()
        let exportDatabase = Hydrate.useExportDatabase ()

        let deleteDatabase =
            Store.useCallback (
                (fun getter setter _ ->
                    promise {
                        Store.change setter Atoms.User.selectedDatabaseIdSet (Set.remove databaseId)
                        do! Store.deleteRoot getter (Atoms.Database.name databaseId)
                        return true
                    }),
                [|
                    box databaseId
                |]
            )

        Menu.Menu
            {|
                Tooltip = ""
                Trigger =
                    InputLabelIconButton.InputLabelIconButton
                        (fun x ->
                            x.``as`` <- UI.react.MenuButton
                            x.icon <- Icons.bs.BsThreeDots |> Icons.render
                            x.fontSize <- "11px"
                            x.disabled <- disabled
                            x.marginLeft <- "6px")
                Body =
                    [
                        if isReadWrite then
                            MenuItem.MenuItem
                                Icons.bs.BsPlus
                                "Add Task"
                                (Some
                                    (fun () ->
                                        navigate (
                                            Navigate.DockPosition.Right,
                                            Some DockType.Task,
                                            UIFlagType.Task,
                                            UIFlag.Task (databaseId, Task.Default.Id)
                                        )))
                                (fun _ -> ())

                            MenuItem.MenuItem
                                Icons.bs.BsPen
                                "Edit Database"
                                (Some
                                    (fun () ->
                                        navigate (
                                            Navigate.DockPosition.Right,
                                            Some DockType.Database,
                                            UIFlagType.Database,
                                            UIFlag.Database databaseId
                                        )))
                                (fun _ -> ())

                        MenuItem.MenuItem Icons.fi.FiCopy "Clone Database" None (fun x -> x.isDisabled <- true)

                        MenuItem.MenuItem
                            Icons.bi.BiExport
                            "Export Database"
                            (Some (fun () -> exportDatabase databaseId))
                            (fun _ -> ())

                        Popover.MenuItemConfirmPopover Icons.bi.BiTrash "Delete Database" deleteDatabase
                    ]
                MenuListProps = fun _ -> ()
            |}
