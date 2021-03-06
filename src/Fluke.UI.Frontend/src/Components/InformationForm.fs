namespace Fluke.UI.Frontend.Components

open Fable.React
open Feliz
open Fluke.Shared.Domain.Model
open Fluke.Shared.Domain.State
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.State
open System
open Fluke.Shared.Domain
open Fluke.Shared


module InformationForm =
    [<ReactComponent>]
    let rec InformationForm information =
        let attachmentIdMap =
            Store.useValue (
                Selectors.Information.attachmentIdMap (
                    information
                    |> Option.defaultValue (Area Area.Default)
                )
            )

        let attachmentIdList =
            React.useMemo (
                (fun () ->
                    attachmentIdMap
                    |> Map.values
                    |> Seq.fold Set.union Set.empty
                    |> Seq.toList),
                [|
                    box attachmentIdMap
                |]
            )

        let setInformationUIFlag = Store.useSetState (Atoms.User.uiFlag UIFlagType.Information)

        let databaseId, setDatabaseId = Store.useState Atoms.User.lastInformationDatabase

        let onAttachmentAdd =
            Store.useCallback (
                (fun _ setter attachmentId ->
                    promise {
                        match databaseId, information with
                        | Some databaseId, Some information ->
                            Store.change
                                setter
                                (Atoms.Database.informationAttachmentIdMap databaseId)
                                (fun informationAttachmentIdMap ->
                                    informationAttachmentIdMap
                                    |> Map.add
                                        information
                                        (informationAttachmentIdMap
                                         |> Map.tryFind information
                                         |> Option.defaultValue Set.empty
                                         |> Set.add attachmentId))
                        | _ -> ()
                    }),
                [|
                    box databaseId
                    box information
                |]
            )

        let onAttachmentDelete =
            Store.useCallback (
                (fun getter setter attachmentId ->
                    promise {
                        let databaseIdSearch =
                            attachmentIdMap
                            |> Map.tryFindKey (fun _ attachmentIdSet -> attachmentIdSet.Contains attachmentId)

                        match databaseIdSearch, information with
                        | Some databaseIdSearch, Some information ->
                            Store.change
                                setter
                                (Atoms.Database.informationAttachmentIdMap databaseIdSearch)
                                (fun informationAttachmentIdMap ->
                                    informationAttachmentIdMap
                                    |> Map.add
                                        information
                                        (informationAttachmentIdMap
                                         |> Map.tryFind information
                                         |> Option.defaultValue Set.empty
                                         |> Set.remove attachmentId))

                            do! Store.deleteRoot getter (Atoms.Attachment.attachment attachmentId)
                            return true
                        | _ -> return false
                    }),
                [|
                    box attachmentIdMap
                    box information
                |]
            )

        Accordion.Accordion
            {|
                Props = fun x -> x.flex <- "1"
                Atom = Atoms.User.accordionHiddenFlag AccordionType.InformationForm
                Items =
                    [
                        str "Info",
                        (UI.stack
                            (fun x -> x.spacing <- "15px")
                            [
                                DatabaseSelector.DatabaseSelector
                                    (databaseId
                                     |> Option.defaultValue Database.Default.Id)
                                    (Some >> setDatabaseId)

                                InformationSelector.InformationSelector
                                    {|
                                        DisableResource = false
                                        SelectionType = InformationSelector.InformationSelectionType.Information
                                        Information = information
                                        OnSelect = UIFlag.Information >> setInformationUIFlag
                                    |}
                            ])

                        match information with
                        | Some information when
                            information
                            |> Information.Name
                            |> InformationName.Value
                            |> String.IsNullOrWhiteSpace
                            |> not
                            ->
                            str "Attachments",
                            (UI.stack
                                (fun x ->
                                    x.spacing <- "10px"
                                    x.flex <- "1")
                                [
                                    AttachmentPanel.AttachmentPanel
                                        AddAttachmentInput.AttachmentPanelType.Information
                                        (if databaseId.IsSome then Some onAttachmentAdd else None)
                                        onAttachmentDelete
                                        attachmentIdList
                                ])
                        | _ -> ()
                    ]
            |}

    [<ReactComponent>]
    let InformationFormWrapper () =
        let informationUIFlag = Store.useValue (Atoms.User.uiFlag UIFlagType.Information)

        let information =
            match informationUIFlag with
            | UIFlag.Information information -> Some information
            | _ -> None

        InformationForm information
