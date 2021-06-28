namespace Fluke.UI.Frontend.Components

open Feliz
open Fable.Core.JsInterop
open Fable.React
open Fluke.UI.Frontend.State
open Fluke.UI.Frontend.Hooks
open Fluke.UI.Frontend.Bindings


module TopBar =
    [<ReactComponent>]
    let TopBar () =
        let deviceInfo = Store.useValue Selectors.deviceInfo
        let logout = Auth.useLogout ()

        let onLogoClick =
            Store.useCallback (
                (fun _ setter _ ->
                    promise {
                        Store.set setter Atoms.leftDock None
                        Store.set setter Atoms.rightDock None
                        Store.set setter Atoms.view Atoms.viewDefault
                    }),
                [||]
            )

        Chakra.flex
            (fun x ->
                x.height <- "29px"
                x.alignItems <- "center"
                x.backgroundColor <- "gray.10"
                x.padding <- "7px"
                x.paddingRight <- "1px"
                x.paddingBottom <- "8px")
            [

                Chakra.flex
                    (fun x ->
                        x.cursor <- "pointer"
                        x.alignItems <- "center"
                        x.onClick <- onLogoClick)
                    [
                        Logo.Logo ()

                        Chakra.box
                            (fun x -> x.marginLeft <- "4px")
                            [
                                str "Fluke"
                            ]
                    ]

                Chakra.spacer (fun _ -> ()) []

                Chakra.stack
                    (fun x ->
                        x.spacing <- "1px"
                        x.alignItems <- "center"
                        x.direction <- "row")
                    [
                        Tooltip.wrap
                            (React.fragment [
                                str "GitHub repository"
                                ExternalLink.externalLinkIcon
                             ])
                            [
                                Chakra.link
                                    (fun x ->
                                        x.href <- "https://github.com/fc1943s/fluke"
                                        x.isExternal <- true
                                        x.display <- "block")
                                    [
                                        TransparentIconButton.TransparentIconButton
                                            {|
                                                Props =
                                                    fun x ->
                                                        x.tabIndex <- -1
                                                        x.icon <- Icons.ai.AiOutlineGithub |> Icons.render
                                                        x.height <- "27px"
                                                        x.fontSize <- "17px"
                                            |}
                                    ]
                            ]

                        Tooltip.wrap
                            (str "Logout")
                            [
                                TransparentIconButton.TransparentIconButton
                                    {|
                                        Props =
                                            fun x ->
                                                x.icon <- Icons.fi.FiLogOut |> Icons.render
                                                x.height <- "27px"
                                                x.fontSize <- "17px"
                                                x.onClick <- fun _ -> promise { do! logout () }
                                    |}
                            ]

                        if deviceInfo.IsElectron then
                            Tooltip.wrap
                                (str "Close")
                                [
                                    TransparentIconButton.TransparentIconButton
                                        {|
                                            Props =
                                                fun x ->
                                                    x.icon <- Icons.io.IoMdClose |> Icons.render
                                                    x.height <- "27px"
                                                    x.fontSize <- "17px"

                                                    x.onClick <-
                                                        fun _ -> promise { Electron.electron.ipcRenderer.send "close" }
                                        |}
                                ]
                    ]
            ]
