namespace rec Fluke.UI.Frontend.Components

open System
open Fable.React
open Feliz
open Fluke.Shared.Domain
open Feliz.Recoil
open Fluke.Shared.Domain.Model
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.State
open Fluke.Shared


module AreaSelector =
    [<ReactComponent>]
    let AreaSelector
        (input: {| Username: UserInteraction.Username
                   Area: Area
                   OnSelect: Area -> unit |})
        =
        let informationSet = Recoil.useValue (Selectors.Session.informationSet input.Username)

        Chakra.box
            (fun x -> x.display <- "inline")
            [
                InputLabel.InputLabel
                    {|
                        Hint =
                            Some (
                                ExternalLink.ExternalLink
                                    {|
                                        Link = str "Read documentation"
                                        Href = "https://fortelabs.co/blog/para/"
                                        Props = fun _ -> ()
                                    |}
                            )
                        HintTitle = None
                        Label = str "Area"
                        Props = fun x -> x.marginBottom <- "5px"
                    |}

                Menu.Menu
                    {|
                        Tooltip = ""
                        Trigger =
                            Chakra.menuButton
                                (fun x ->
                                    x.``as`` <- Chakra.react.Button
                                    x.rightIcon <- Icons.fi.FiChevronDown |> Icons.render)
                                [
                                    match input.Area.Name |> AreaName.Value with
                                    | String.ValidString name -> str name
                                    | _ -> str "Select..."
                                ]
                        Menu =
                            [
                                Chakra.box
                                    (fun x ->
                                        x.marginBottom <- "6px"
                                        x.maxHeight <- "217px"
                                        x.overflowY <- "auto"
                                        x.flexBasis <- 0)
                                    [
                                        Chakra.menuOptionGroup
                                            (fun x -> x.value <- input.Area)
                                            [
                                                yield!
                                                    (if input.Area.Name
                                                        |> AreaName.Value
                                                        |> String.IsNullOrWhiteSpace then
                                                         informationSet
                                                     else
                                                         informationSet |> Set.add (Area input.Area))
                                                    |> Set.toList
                                                    |> List.map
                                                        (function
                                                        | Area area ->
                                                            let label = area.Name |> AreaName.Value

                                                            let cmp =
                                                                Chakra.menuItemOption
                                                                    (fun x ->
                                                                        x.value <- area

                                                                        x.onClick <-
                                                                            fun _ -> promise { input.OnSelect area })
                                                                    [
                                                                        str label
                                                                    ]

                                                            Some (label, cmp)
                                                        | _ -> None)
                                                    |> List.sortBy (Option.map fst)
                                                    |> List.map (Option.map snd)
                                                    |> List.map (Option.defaultValue nothing)
                                            ]
                                    ]

                                Chakra.box
                                    (fun x -> x.textAlign <- "center")
                                    [
                                        ModalForm.ModalFormTrigger
                                            {|
                                                Username = input.Username
                                                Trigger =
                                                    fun trigger _ ->

                                                        Button.Button
                                                            {|
                                                                Hint = None
                                                                Icon =
                                                                    Some (
                                                                        Icons.bs.BsPlus |> Icons.wrap,
                                                                        Button.IconPosition.Left
                                                                    )
                                                                Props =
                                                                    fun x ->
                                                                        x.onClick <- fun _ -> promise { trigger () }
                                                                Children =
                                                                    [
                                                                        str "Add Area"
                                                                    ]
                                                            |}
                                                TextKey = TextKey (nameof AreaForm)
                                                TextKeyValue = None
                                            |}

                                        ModalForm.ModalForm
                                            {|
                                                Username = input.Username
                                                Content =
                                                    fun (_, onHide, _) ->
                                                        AreaForm.AreaForm
                                                            {|
                                                                Username = input.Username
                                                                Area = input.Area
                                                                OnSave =
                                                                    fun area ->
                                                                        promise {
                                                                            input.OnSelect area
                                                                            onHide ()
                                                                        }
                                                            |}
                                                TextKey = TextKey (nameof AreaForm)
                                            |}
                                    ]
                            ]
                        MenuListProps = fun x -> x.padding <- "10px"
                    |}
            ]
