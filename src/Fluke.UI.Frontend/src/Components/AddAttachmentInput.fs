namespace Fluke.UI.Frontend.Components

open System
open Fable.React
open Fluke.Shared.Domain.UserInteraction
open Fluke.UI.Frontend.Components
open Fluke.UI.Frontend.Bindings
open Fluke.Shared
open Fluke.UI.Frontend.Hooks
open Fluke.UI.Frontend.State
open Feliz
open Fluke.Shared.Domain


module AddAttachmentInput =
    [<RequireQualifiedAccess>]
    type AttachmentPanelType =
        | Information
        | Task
        | Cell


    [<ReactComponent>]
    let rec AddAttachmentInput attachmentPanelType onAdd =
        let archive = Store.useValue Atoms.User.archive
        let ctrlPressed = Store.useValue Atoms.ctrlPressed

        let tempAttachment =
            Store.Hooks.useTempAtom
                (Some (
                    Store.InputAtom (Store.AtomReference.Atom (Atoms.Attachment.attachment (AttachmentId Guid.Empty)))
                ))
                (Some (Store.InputScope.Temp Gun.defaultSerializer))

        let clipboardVisible, setClipboardVisible = Store.useState Atoms.User.clipboardVisible
        let clipboardAttachmentIdMap = Store.useValue Atoms.User.clipboardAttachmentIdMap

        let addAttachment =
            Store.useCallback (
                (fun getter setter _ ->
                    promise {
                        match onAdd, tempAttachment.Value with
                        | Some onAdd, Some (Attachment.Comment (Comment.Comment (String.ValidString _)) as attachment) ->

                            let attachmentId =
                                Hydrate.hydrateAttachmentState
                                    getter
                                    setter
                                    (Store.AtomScope.Current,
                                     {
                                         Timestamp = DateTime.Now |> FlukeDateTime.FromDateTime
                                         Archived =
                                             match attachmentPanelType with
                                             | AttachmentPanelType.Information -> archive |> Option.defaultValue false
                                             | _ -> false
                                         Attachment = attachment
                                     })

                            do! onAdd attachmentId

                            // Store.resetTempValue setter (Atoms.Attachment.attachment (AttachmentId Guid.Empty))
                            tempAttachment.SetValue (Some (Attachment.Comment (Comment.Comment "")))
                        | _ -> ()
                    }),
                [|
                    box attachmentPanelType
                    box archive
                    box onAdd
                    box tempAttachment
                |]
            )

        UI.stack
            (fun x -> x.spacing <- "0")
            [
                UI.flex
                    (fun _ -> ())
                    [

                        Tooltip.wrap
                            (if onAdd.IsNone then str "No database selected" else nothing)
                            [
                                Input.LeftIconInput
                                    {|
                                        Icon = Icons.fi.FiPaperclip |> Icons.render
                                        CustomProps =
                                            fun x ->
                                                x.textarea <- true

                                                x.atom <-
                                                    Some (
                                                        Store.InputAtom (
                                                            Store.AtomReference.Atom (
                                                                Atoms.Attachment.attachment (AttachmentId Guid.Empty)
                                                            )
                                                        )
                                                    )

                                                x.inputScope <- Some (Store.InputScope.Temp Gun.defaultSerializer)
                                                x.autoFocusOnAllMounts <- true
                                                x.variableHeight <- true

                                                x.onEnterPress <-
                                                    Some (fun _ -> promise { if ctrlPressed then do! addAttachment () })

                                                x.onFormat <-
                                                    Some
                                                        (fun attachment ->
                                                            match attachment with
                                                            | Some (Attachment.Comment (Comment.Comment comment)) ->
                                                                comment
                                                            | attachment -> $"{attachment}")

                                                x.onValidate <-
                                                    Some (
                                                        fst
                                                        >> Comment.Comment
                                                        >> Attachment.Comment
                                                        >> Some
                                                        >> Some
                                                    )
                                        Props =
                                            fun x ->
                                                x.placeholder <- "Add Attachment"
                                                x.autoFocus <- true
                                                x.maxHeight <- "200px"
                                                x.borderBottomRightRadius <- "0"
                                                x.borderTopRightRadius <- "0"

                                                if onAdd.IsNone then x.disabled <- true
                                    |}
                            ]

                        UI.stack
                            (fun x ->
                                x.spacing <- "0"
                                x.paddingTop <- "1px"
                                x.paddingBottom <- "1px")
                            [
                                Button.Button
                                    {|
                                        Hint = None
                                        Icon = Some (Icons.fa.FaPlus |> Icons.render, Button.IconPosition.Left)
                                        Props =
                                            fun x ->
                                                UI.setTestId x "Add Attachment"
                                                x.borderBottomLeftRadius <- "0"
                                                x.borderTopLeftRadius <- "0"
                                                if onAdd.IsNone then x.disabled <- true

                                                x.onClick <- fun _ -> addAttachment ()
                                        Children = []
                                    |}

                                UI.spacer (fun _ -> ()) []

                                Tooltip.wrap
                                    (str "Clipboard")
                                    [
                                        UI.box
                                            (fun _ -> ())
                                            [
                                                Button.Button
                                                    {|
                                                        Hint = None
                                                        Icon =
                                                            Some (
                                                                Icons.io5.IoDocumentAttachOutline |> Icons.render,
                                                                Button.IconPosition.Left
                                                            )
                                                        Props =
                                                            fun x ->
                                                                UI.setTestId x "Clipboard"

                                                                if onAdd.IsNone then x.disabled <- true
                                                                x.borderBottomLeftRadius <- "0"
                                                                x.borderTopLeftRadius <- "0"

                                                                x.onClick <-
                                                                    fun _ ->
                                                                        promise {
                                                                            setClipboardVisible (not clipboardVisible) }
                                                        Children =
                                                            [
                                                                UI.box
                                                                    (fun _ -> ())
                                                                    [
                                                                        str (string clipboardAttachmentIdMap.Count)
                                                                    ]
                                                            ]
                                                    |}
                                            ]
                                    ]
                            ]
                    ]
                match onAdd with
                | Some onAdd -> AttachmentsClipboard.AttachmentsClipboard onAdd
                | None -> nothing
            ]
