namespace rec Fluke.UI.Frontend.Components

open Fable.Core.JsInterop
open System
open Fable.React
open Feliz
open Fluke.Shared.Domain
open Fluke.Shared.Domain.Model
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.State
open Fluke.Shared


module InformationSelector =
    [<RequireQualifiedAccess>]
    type InformationSelectionType =
        | Information
        | Project
        | Area
        | Resource

    let isVisibleInformation informationString information =
        match information with
        | information when
            information
            |> Information.Name
            |> InformationName.Value
            |> String.IsNullOrWhiteSpace -> false
        | information when
            information |> Information.isProject
            && informationString = nameof Project -> true
        | information when
            information |> Information.isArea
            && informationString = nameof Area -> true
        | information when
            information |> Information.isResource
            && informationString = nameof Resource -> true
        | _ -> false

    [<ReactComponent>]
    let InformationSelector
        (input: {| DisableResource: bool
                   SelectionType: InformationSelectionType
                   TaskId: TaskId |})
        =
        let tempInformation =
            Store.Hooks.useTempAtom
                (Some (Store.InputAtom (Store.AtomReference.Atom (Atoms.Task.information input.TaskId))))
                (Some (Store.InputScope.ReadWrite Gun.defaultSerializer))

        let informationName, informationSelected =
            React.useMemo (
                (fun () ->
                    tempInformation.Value
                    |> Information.Name
                    |> InformationName.Value,

                    tempInformation.Value
                    |> Information.toString),
                [|
                    box tempInformation.Value
                |]
            )

        let selected, setSelected =
            React.useState (
                informationName
                |> String.IsNullOrWhiteSpace
                |> not
            )

        React.useEffect (
            (fun () -> if not selected && informationName.Length > 0 then setSelected true),
            [|
                box selected
                box informationName
                box setSelected
            |]
        )

        let informationSet = Store.useValue Selectors.Session.informationSet

        let sortedInformationList =
            React.useMemo (
                (fun () ->
                    informationSet
                    |> Set.add tempInformation.Value
                    |> Set.filter (isVisibleInformation informationSelected)
                    |> Set.toList),
                [|
                    box informationSelected
                    box informationSet
                    box tempInformation.Value
                |]
            )

        let index =
            React.useMemo (
                (fun () ->
                    sortedInformationList
                    |> List.sort
                    |> List.tryFindIndex ((=) tempInformation.Value)
                    |> Option.defaultValue -1),
                [|
                    box sortedInformationList
                    box tempInformation.Value
                |]
            )

        let isTesting = Store.useValue Store.Atoms.isTesting

        Chakra.box
            (fun x ->
                x.display <- "inline"
                if isTesting then x?``data-testid`` <- nameof InformationSelector)
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
                        Label = str "Information"
                        Props = fun x -> x.marginBottom <- "5px"
                    |}

                Dropdown.Dropdown
                    {|
                        Tooltip = ""
                        Left = true
                        Trigger =
                            fun visible setVisible ->
                                Button.Button
                                    {|
                                        Hint = None
                                        Icon =
                                            Some (
                                                (if visible then Icons.fi.FiChevronUp else Icons.fi.FiChevronDown)
                                                |> Icons.wrap,
                                                Button.IconPosition.Right
                                            )
                                        Props = fun x -> x.onClick <- fun _ -> promise { setVisible (not visible) }
                                        Children =
                                            [
                                                match informationName with
                                                | String.ValidString _ ->
                                                    str $"{informationSelected}: {informationName}"
                                                | _ -> str "Select..."
                                            ]
                                    |}
                        Body =
                            fun onHide ->
                                [
                                    match input.SelectionType with
                                    | InformationSelectionType.Information ->
                                        Chakra.radioGroup
                                            (fun x ->
                                                x.onChange <-
                                                    fun (radioValueSelected: string) ->
                                                        promise {
                                                            if tempInformation.Value
                                                               |> isVisibleInformation radioValueSelected
                                                               |> not then
                                                                match radioValueSelected with
                                                                | nameof Project ->
                                                                    tempInformation.SetValue (
                                                                        Project Project.Default
                                                                    )
                                                                | nameof Area ->
                                                                    tempInformation.SetValue (
                                                                        Area Area.Default
                                                                    )
                                                                | nameof Resource ->
                                                                    tempInformation.SetValue (
                                                                        Resource Resource.Default
                                                                    )
                                                                | _ -> ()

                                                            setSelected true
                                                        }

                                                x.value <- if not selected then null else informationSelected)
                                            [
                                                Chakra.stack
                                                    (fun x ->
                                                        x.justifyContent <- "center"
                                                        x.flex <- "1"
                                                        x.spacing <- "15px"
                                                        x.direction <- "row")
                                                    [
                                                        let label text =
                                                            Chakra.box
                                                                (fun x -> x.marginBottom <- "-2px")
                                                                [
                                                                    str text
                                                                ]

                                                        Radio.Radio
                                                            (fun x -> x.value <- nameof Project)
                                                            [
                                                                label "Project"
                                                            ]

                                                        Radio.Radio
                                                            (fun x -> x.value <- nameof Area)
                                                            [
                                                                label "Area"
                                                            ]

                                                        Radio.Radio
                                                            (fun x ->
                                                                x.value <- nameof Resource
                                                                x.isDisabled <- input.DisableResource)
                                                            [
                                                                Tooltip.wrap
                                                                    (if input.DisableResource then
                                                                         str "Tasks can't be assigned to Resources"
                                                                     else
                                                                         nothing)
                                                                    [
                                                                        label "Resource"
                                                                    ]
                                                            ]
                                                    ]
                                            ]
                                    | _ -> nothing

                                    match selected, informationSelected with
                                    | false, _ -> None
                                    | _, nameof Project -> Some (TextKey (nameof ProjectForm))
                                    | _, nameof Area -> Some (TextKey (nameof AreaForm))
                                    | _, nameof Resource -> Some (TextKey (nameof AreaForm))
                                    | _ -> None
                                    |> function
                                    | Some formTextKey ->
                                        React.fragment [
                                            Chakra.stack
                                                (fun x ->
                                                    x.flex <- "1"
                                                    x.spacing <- "1px"
                                                    x.padding <- "1px"
                                                    x.marginBottom <- "6px"
                                                    x.marginTop <- "10px"
                                                    x.maxHeight <- "217px"
                                                    x.overflowY <- "auto"
                                                    x.flexBasis <- 0)
                                                [
                                                    yield!
                                                        sortedInformationList
                                                        |> List.mapi
                                                            (fun i information ->

                                                                Button.Button
                                                                    {|
                                                                        Hint = None
                                                                        Icon =
                                                                            Some (
                                                                                (if index = i then
                                                                                     Icons.fi.FiCheck |> Icons.wrap
                                                                                 else
                                                                                     fun () ->
                                                                                         (Chakra.box
                                                                                             (fun x ->
                                                                                                 x.width <- "11px")
                                                                                             [])),
                                                                                Button.IconPosition.Left
                                                                            )
                                                                        Props =
                                                                            fun x ->
                                                                                x.onClick <-
                                                                                    fun _ ->
                                                                                        promise {
                                                                                            tempInformation.SetValue
                                                                                                information

                                                                                            onHide ()
                                                                                        }

                                                                                x.alignSelf <- "stretch"
                                                                                x.backgroundColor <- "whiteAlpha.100"
                                                                                x.borderRadius <- "2px"
                                                                        Children =
                                                                            [
                                                                                information
                                                                                |> Information.Name
                                                                                |> InformationName.Value
                                                                                |> str
                                                                            ]
                                                                    |})
                                                ]

                                            Dropdown.Dropdown
                                                {|
                                                    Tooltip = ""
                                                    Left = true
                                                    Trigger =
                                                        fun visible setVisible ->
                                                            Button.Button
                                                                {|
                                                                    Hint = None
                                                                    Icon =
                                                                        Some (
                                                                            (if visible then
                                                                                 Icons.fi.FiChevronUp
                                                                             else
                                                                                 Icons.fi.FiChevronDown)
                                                                            |> Icons.wrap,
                                                                            Button.IconPosition.Right
                                                                        )
                                                                    Props =
                                                                        fun x ->
                                                                            x.onClick <-
                                                                                fun _ ->
                                                                                    promise { setVisible (not visible) }
                                                                    Children =
                                                                        [
                                                                            match informationSelected with
                                                                            | nameof Project -> "Add Project"
                                                                            | nameof Area -> "Add Area"
                                                                            | nameof Resource -> "Add Resource"
                                                                            | _ -> ""
                                                                            |> str
                                                                        ]
                                                                |}
                                                    Body =
                                                        fun onHide2 ->
                                                            [
                                                                match informationSelected with
                                                                | nameof Project ->
                                                                    ProjectForm.ProjectForm
                                                                        (match tempInformation.Value with
                                                                         | Project project -> project
                                                                         | _ -> Project.Default)
                                                                        (fun project ->
                                                                            promise {
                                                                                tempInformation.SetValue (
                                                                                    Project project
                                                                                )

                                                                                onHide ()
                                                                                onHide2 ()
                                                                            })
                                                                | nameof Area ->
                                                                    AreaForm.AreaForm
                                                                        (match tempInformation.Value with
                                                                         | Area area -> area
                                                                         | _ -> Area.Default)
                                                                        (fun area ->
                                                                            promise {
                                                                                tempInformation.SetValue (
                                                                                    Area area
                                                                                )

                                                                                onHide ()
                                                                                onHide2 ()
                                                                            })
                                                                | nameof Resource -> nothing
                                                                | _ -> nothing
                                                            ]
                                                |}
                                        ]
                                    | _ -> nothing
                                ]
                    |}
            ]
