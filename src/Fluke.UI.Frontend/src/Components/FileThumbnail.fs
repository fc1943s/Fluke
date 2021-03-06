namespace Fluke.UI.Frontend.Components

open Fluke.Shared.Domain.UserInteraction
open Fluke.UI.Frontend.Components
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.State
open Feliz


module FileThumbnail =
    [<ReactComponent>]
    let ImageThumbnail children =
        UI.flex
            (fun x ->
                x.width <- "75px"
                x.height <- "75px"
                x.overflow <- "overlay"
                x.justifyContent <- "center"
                x.borderWidth <- "1px"
                x.borderColor <- "gray.16"
                x.alignItems <- "center")
            [
                yield! children
            ]

    [<ReactComponent>]
    let FileThumbnail fileId =
        let objectUrl = Store.useValue (Selectors.File.objectUrl fileId)

        ImageThumbnail
            [
                match objectUrl with
                | Some url ->
                    ImageModal.ImageModal UIFlagType.File (UIFlag.File fileId) $"File ID: {fileId |> FileId.Value}" url
                | None -> LoadingSpinner.InlineLoadingSpinner ()
            ]
