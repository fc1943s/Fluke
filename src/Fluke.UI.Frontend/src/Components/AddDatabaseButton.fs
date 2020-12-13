namespace Fluke.UI.Frontend.Components

open Fable.React
open Feliz
open Feliz.Recoil
open Feliz.UseListener
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend

module AddDatabaseButton =

    [<ReactComponent>]
    let AddDatabaseButton (input: {| props: {| marginLeft: string |} |}) =
        let username = Recoil.useValue Recoil.Atoms.username
        let formDatabaseId, setFormDatabaseId = Recoil.useState (Recoil.Atoms.formDatabaseId)



        React.fragment [
            Button.Button
                {|
                    Icon = Icons.faPlus
                    RightIcon = false
                    props =
                        {|
                            marginLeft = input.props.marginLeft
                            onClick = fun () -> setFormDatabaseId (Some (Recoil.Atoms.Database.newDatabaseId ()))
                        |}
                    children =
                        [
                            str "Add Database"
                        ]
                |}

            Modal.Modal
                {|
                    IsOpen = formDatabaseId.IsSome
                    OnClose = fun () -> setFormDatabaseId None
                    children =
                        [
                            match formDatabaseId, username with
                            | Some databaseId, Some username ->
                                EditDatabase.EditDatabase
                                    {|
                                        Username = username
                                        DatabaseId = databaseId
                                        OnSave = async { setFormDatabaseId None }
                                    |}
                            | _ -> ()
                        ]
                |}
        ]
