namespace Fluke.UI.Frontend.Components

open System
open Fable.Core.JsInterop
open Fable.Core
open Fable.Extras
open Fluke.Shared
open Fluke.Shared.Domain.State
open Feliz
open Fable.React
open Fluke.UI.Frontend.Hooks
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.State
open Fluke.UI.Frontend.TempUI
open Browser.Types


module Databases =
    let icons =
        {|
            check =
                UI.icon
                    (fun x ->
                        x.``as`` <- Icons.md.MdCheckBox
                        x.tabIndex <- 0
                        x.marginLeft <- "-41px"
                        x.marginRight <- "2px"
                        x.height <- "19px"
                        x.width <- "19px"
                        x.color <- "gray.87")
                    []
            halfCheck =
                UI.icon
                    (fun x ->
                        x.``as`` <- Icons.md.MdIndeterminateCheckBox
                        x.tabIndex <- 0
                        x.marginLeft <- "-41px"
                        x.marginRight <- "2px"
                        x.height <- "19px"
                        x.width <- "19px"
                        x.color <- "gray.87")
                    []
            uncheck =
                UI.icon
                    (fun x ->
                        x.``as`` <- Icons.md.MdCheckBoxOutlineBlank
                        x.tabIndex <- 0
                        x.marginLeft <- "-41px"
                        x.marginRight <- "2px"
                        x.height <- "19px"
                        x.width <- "19px"
                        x.color <- "gray.87")
                    []
            expandOpen =
                UI.icon
                    (fun x ->
                        x.``as`` <- Icons.fa.FaChevronDown
                        x.height <- "16px"
                        x.width <- "16px"
                        x.fontSize <- "14px"
                        x.color <- "gray.87")
                    []
            expandClose =
                UI.icon
                    (fun x ->
                        x.``as`` <- Icons.fa.FaChevronRight
                        x.height <- "16px"
                        x.width <- "16px"
                        x.fontSize <- "14px"
                        x.color <- "gray.87")
                    []
            parentClose =
                UI.icon
                    (fun x ->
                        x.``as`` <- Icons.fi.FiDatabase
                        x.marginLeft <- "-11px"
                        x.color <- "gray.87")
                    []
            parentOpen =
                UI.icon
                    (fun x ->
                        x.``as`` <- Icons.fi.FiDatabase
                        x.marginLeft <- "-11px"
                        x.color <- "gray.87")
                    []
        |}

    type CheckboxTreeNode =
        {
            children: CheckboxTreeNode [] option
            disabled: bool
            icon: obj
            label: ReactElement
            value: string
        }

    let node
        (input: {| DatabaseId: DatabaseId option
                   Disabled: bool
                   IsTesting: bool
                   Value: string
                   IsEmptyNode: bool
                   Label: string
                   Children: CheckboxTreeNode []
                   Icon: obj |})
        =
        let disabled = input.Disabled || input.Value = ""

        let labelText =
            if input.Children = JS.undefined then
                Some ((JSe.RegExp @"^(.*? )([^ ]+)$").Match input.Label)
            else
                None

        {
            value = input.Value
            disabled = input.IsEmptyNode || disabled
            children = Some input.Children
            icon =
                if input.IsEmptyNode then
                    box (
                        UI.box
                            (fun x ->
                                x.marginLeft <- "-10px"
                                x.height <- "10px")
                            []
                    )
                else
                    input.Icon
            label =
                Tooltip.wrap
                    (if input.Disabled then
                         (str "There are databases selected with a different position")
                     else
                         nothing)
                    [
                        UI.box
                            (fun x ->
                                x.display <- "inline"
                                x.marginLeft <- "-2px"
                                x.lineHeight <- "19px")
                            [
                                UI.box
                                    (fun x ->
                                        x.fontSize <- "main"
                                        x.paddingTop <- "1px"
                                        x.paddingBottom <- "1px"
                                        x.marginLeft <- if input.DatabaseId.IsNone then "2px" else null
                                        x.display <- "inline")
                                    [
                                        match labelText with
                                        | Some label -> str (label |> Seq.item 1)
                                        | _ -> str input.Label
                                    ]

                                match input.DatabaseId with
                                | Some databaseId ->
                                    UI.box
                                        (fun x ->
                                            x.display <- "inline"
                                            x.fontSize <- "main"

                                            x.visibility <-
                                                if databaseId = Database.Default.Id then "hidden" else "visible"

                                            x.whiteSpace <- "nowrap")
                                        [
                                            match labelText with
                                            | Some label ->
                                                UI.box
                                                    (fun x -> x.display <- "inline")
                                                    [
                                                        str (label |> Seq.item 2)
                                                    ]
                                            | None -> nothing

                                            DatabaseNodeMenu.DatabaseNodeMenu databaseId disabled
                                        ]
                                | _ -> nothing

                                match input.IsEmptyNode, input.DatabaseId with
                                | _, Some _
                                | true, _ -> UI.box (fun x -> x.height <- "14px") []
                                | _ -> nothing
                            ]
                    ]
        }

    [<RequireQualifiedAccess>]
    type NodeValue =
        | Empty
        | Parent of Guid
        | Leaf of Guid

    type Node = Node of value: NodeValue * label: string * children: Node [] * index: int option

    let buildNodesFromPath (ids: Guid option []) (paths: string []) =
        let rec groupNodes nodes =
            nodes
            |> Array.groupBy (fun (Node (_, label, _, _)) -> label)
            |> Array.map
                (fun (label, nodes) ->
                    let (Node (value, _, _, index)) = nodes.[0]

                    Node (
                        value,
                        label,
                        (nodes
                         |> Array.collect (fun (Node (_, _, children, _)) -> children)
                         |> groupNodes),
                        index
                    ))

        paths
        |> Array.map
            (fun path ->
                path.Replace("\/", "|||").Split "/"
                |> Array.map (fun str -> str.Replace ("|||", "/"))
                |> Array.toList)
        |> Array.mapi
            (fun i nodes ->
                let rec loop depth list =
                    match list with
                    | [ String.ValidString head ] ->
                        let id =
                            match ids |> Array.tryItem i with
                            | Some (Some id) -> NodeValue.Leaf id
                            | _ -> NodeValue.Empty

                        Node (id, head, [||], Some i)
                    | String.ValidString head :: tail ->
                        let fullPath = nodes |> List.take depth |> String.concat "/"
                        let nodeId = fullPath |> Crypto.getTextGuidHash

                        Node (
                            NodeValue.Parent nodeId,
                            head,
                            [|
                                loop (depth + 1) tail
                            |],
                            None
                        )
                    | _ -> Node (NodeValue.Empty, "", [||], None)

                loop 1 nodes)
        |> groupNodes


    [<ReactComponent>]
    let AddDatabaseButton () =
        let setLeftDock = Store.useSetState Atoms.User.leftDock
        let setRightDock = Store.useSetState Atoms.User.rightDock
        let deviceInfo = Store.useValue Selectors.deviceInfo
        let setDatabaseUIFlag = Store.useSetState (Atoms.User.uiFlag UIFlagType.Database)

        Tooltip.wrap
            (str "Add Database")
            [
                TransparentIconButton.TransparentIconButton
                    {|
                        Props =
                            fun x ->
                                UI.setTestId x "Add Database"
                                x.icon <- Icons.fi.FiPlus |> Icons.render
                                x.fontSize <- "17px"

                                x.onClick <-
                                    fun _ ->
                                        promise {
                                            if deviceInfo.IsMobile then setLeftDock None
                                            setRightDock (Some DockType.Database)
                                            setDatabaseUIFlag UIFlag.None
                                        }
                    |}
            ]

    [<ReactComponent>]
    let ImportDatabase () =
        let inputRef = React.useRef<HTMLInputElement> null
        let files, setFiles = React.useState (None: FileList option)
        let importDatabase = Hydrate.useImportDatabase ()

        UI.stack
            (fun x -> x.spacing <- "15px")
            [
                UI.box
                    (fun x ->
                        x.paddingBottom <- "5px"
                        x.marginRight <- "24px"
                        x.fontSize <- "1.3rem")
                    [
                        str "Import Database"
                    ]

                UI.input
                    (fun x ->
                        x.``type`` <- "file"
                        x.padding <- "10px"
                        x.height <- "43px"
                        x.ref <- inputRef
                        x.onChange <- fun x -> promise { x?target?files |> Option.ofObj |> setFiles })
                    []

                UI.box
                    (fun _ -> ())
                    [
                        Button.Button
                            {|
                                Hint = None
                                Icon = Some (Icons.bi.BiImport |> Icons.render, Button.IconPosition.Left)
                                Props =
                                    fun x ->
                                        x.onClick <-
                                            fun _ ->
                                                promise {
                                                    do! importDatabase files
                                                    inputRef.current.value <- ""
                                                }
                                Children =
                                    [
                                        str "Confirm"
                                    ]
                            |}
                    ]
            ]


    [<ReactComponent>]
    let rec Databases props =
        let databaseIdAtoms = Store.useValue Selectors.databaseIdAtoms

        let databaseIdArray =
            databaseIdAtoms
            |> Store.waitForAll
            |> Store.useValue

        let databaseArray =
            databaseIdArray
            |> Array.map Selectors.Database.database
            |> Store.waitForAll
            |> Store.useValue

        let databaseNodeTypeArray =
            databaseIdArray
            |> Array.map Selectors.Database.nodeType
            |> Store.waitForAll
            |> Store.useValue

        let databaseMap =
            React.useMemo (
                (fun () ->
                    databaseArray
                    |> Array.map (fun database -> database.Id, database)
                    |> Map.ofSeq),
                [|
                    box databaseArray
                |]
            )

        let hideTemplates = Store.useValue Atoms.User.hideTemplates
        let hideTemplatesCache = React.useRef<bool option> None

        let expandedDatabaseIdSet, setExpandedDatabaseIdSet = Store.useState Atoms.User.expandedDatabaseIdSet

        React.useEffect (
            (fun () ->
                match hideTemplates, hideTemplatesCache.current with
                | Some true,
                  (None
                  | Some false) -> setExpandedDatabaseIdSet Set.empty
                | _ -> ()

                hideTemplatesCache.current <- hideTemplates),
            [|
                box setExpandedDatabaseIdSet
                box hideTemplates
                box hideTemplatesCache
            |]
        )

        let nodeData =
            React.useMemo (
                (fun () ->
                    let databaseMapByType =
                        databaseArray
                        |> Array.mapi
                            (fun i database ->
                                let nodeType = databaseNodeTypeArray.[i]
                                nodeType, database)
                        |> Array.groupBy fst
                        |> Map.ofSeq
                        |> Map.map (fun _ v -> v |> Array.map snd)

                    [|
                        if hideTemplates = Some false then yield DatabaseNodeType.Template
                        yield DatabaseNodeType.Owned
                        yield DatabaseNodeType.Shared
                    |]
                    |> Array.collect
                        (fun nodeType ->
                            let newDatabaseNameArray =
                                databaseMapByType
                                |> Map.tryFind nodeType
                                |> Option.map (Array.map (fun database -> Some database, database.Name))
                                |> Option.defaultValue [|
                                    None, DatabaseName "None"
                                   |]

                            let prefix =
                                match nodeType with
                                | DatabaseNodeType.Template -> "Templates\/Unit Tests"
                                | DatabaseNodeType.Owned -> "Created by me"
                                | DatabaseNodeType.Shared -> "Shared with me"

                            newDatabaseNameArray
                            |> Array.map
                                (fun (database, databaseName) ->
                                    database, $"{prefix}/{databaseName |> DatabaseName.Value}")
                            |> Array.sortBy snd)),
                [|
                    box databaseNodeTypeArray
                    box databaseArray
                    box hideTemplates
                |]
            )

        let isTesting = Store.useValue Store.Atoms.isTesting
        let selectedDatabaseIdSet, setSelectedDatabaseIdSet = Store.useState Atoms.User.selectedDatabaseIdSet

        let nodes, newExpandedDatabaseGuidArray, newSelectedDatabaseGuidArray =
            React.useMemo (
                (fun () ->
                    let nodes =
                        let ids =
                            nodeData
                            |> Array.map fst
                            |> Array.map (Option.map (fun x -> x.Id |> DatabaseId.Value))

                        let paths = nodeData |> Array.map snd
                        buildNodesFromPath ids paths |> Array.toList

                    let rec loop nodes =
                        match nodes with
                        | Node (value, label, children, index) :: tail ->
                            let nodeChildren =
                                match children with
                                | [||] -> JS.undefined
                                | _ -> loop (children |> Array.toList) |> List.toArray

                            let database =
                                match index with
                                | Some index -> nodeData.[index] |> fst
                                | _ -> None

                            let icon =
                                match database with
                                | Some database -> DatabaseLeafIcon.DatabaseLeafIcon database.Id
                                | _ -> JS.undefined

                            let disabled =
                                match database with
                                | Some database ->
                                    let validSelectedDatabases =
                                        selectedDatabaseIdSet
                                        |> Set.map (fun databaseId -> databaseMap |> Map.tryFind databaseId)

                                    match database.Position with
                                    | Some position ->
                                        validSelectedDatabases
                                        |> Set.exists
                                            (function
                                            | Some database ->
                                                database.Position.IsNone
                                                || database.Position <> (Some position)
                                            | None -> false)
                                    | None ->
                                        validSelectedDatabases
                                        |> Set.exists
                                            (function
                                            | Some database -> database.Position.IsSome
                                            | None -> false)
                                | _ -> false

                            let newValue =
                                database
                                |> Option.map (fun database -> database.Id |> DatabaseId.Value)
                                |> Option.defaultWith
                                    (fun () ->
                                        match value with
                                        | NodeValue.Empty -> None
                                        | NodeValue.Parent guid -> Some guid
                                        | NodeValue.Leaf guid -> Some guid
                                        |> Option.defaultWith (DatabaseId.NewId >> DatabaseId.Value))
                                |> string

                            let isEmptyNode =
                                match value with
                                | NodeValue.Empty _ -> true
                                | _ -> false

                            let newNode =
                                node
                                    {|
                                        DatabaseId =
                                            database
                                            |> Option.map (fun database -> database.Id)
                                        Disabled = disabled
                                        IsTesting = isTesting
                                        Value = newValue
                                        IsEmptyNode = isEmptyNode
                                        Label = label
                                        Children = nodeChildren
                                        Icon = icon
                                    |}

                            newNode :: loop tail
                        | [] -> []

                    let nodes = loop nodes |> List.toArray

                    let newExpandedDatabaseGuidArray =
                        (if expandedDatabaseIdSet.IsEmpty then
                             nodes
                             |> Array.map (fun node -> node.value |> Guid |> DatabaseId)
                         else
                             expandedDatabaseIdSet |> Set.toArray)
                        |> Array.map DatabaseId.Value

                    let newSelectedDatabaseGuidArray =
                        selectedDatabaseIdSet
                        |> Set.toArray
                        |> Array.map DatabaseId.Value

                    nodes, newExpandedDatabaseGuidArray, newSelectedDatabaseGuidArray),
                [|
                    box nodeData
                    box databaseMap
                    box expandedDatabaseIdSet
                    box selectedDatabaseIdSet
                    box isTesting
                |]
            )

        match JS.window id with
        | Some window -> window?nodes <- nodes
        | None -> ()

        let checkboxTreeProps =
            React.useMemo (
                (fun () ->
                    {|
                        ``checked`` = newSelectedDatabaseGuidArray
                        expanded = newExpandedDatabaseGuidArray
                        onCheck =
                            (fun (x: string []) ->
                                x
                                |> Array.map (Guid >> DatabaseId)
                                |> Set.ofSeq
                                |> setSelectedDatabaseIdSet)
                        onExpand =
                            (fun (x: string []) ->
                                x
                                |> Array.map (Guid >> DatabaseId)
                                |> Set.ofSeq
                                |> setExpandedDatabaseIdSet)
                        expandOnClick = true
                        onlyLeafCheckboxes = true
                        nodes = nodes
                        icons = icons
                    |}),
                [|
                    box newSelectedDatabaseGuidArray
                    box newExpandedDatabaseGuidArray
                    box setSelectedDatabaseIdSet
                    box setExpandedDatabaseIdSet
                    box nodes
                |]
            )

        UI.stack
            props
            [
                UI.flex
                    (fun x ->
                        x.marginLeft <- "6px"
                        x.flex <- "1")
                    [
                        match hideTemplates with
                        | Some _ -> CheckboxTree.render checkboxTreeProps
                        | None -> LoadingSpinner.LoadingSpinner ()
                    ]
            ]
