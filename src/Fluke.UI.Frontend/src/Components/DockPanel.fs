namespace Fluke.UI.Frontend.Components

open Fable.React
open Feliz
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Bindings


module DockPanel =
    [<RequireQualifiedAccess>]
    type DockPanelIcon =
        | Component of cmp: ReactElement
        | Menu of title: string * icon: ReactElement * children: seq<ReactElement>

    [<ReactComponent>]
    let DockPanel
        (input: {| Name: string
                   Icon: obj
                   Atom: Recoil.RecoilValue<TempUI.DockType option, Recoil.ReadWrite>
                   RightIcons: DockPanelIcon list
                   children: seq<ReactElement> |})
        =
        let setAtom = Store.useSetState input.Atom

        Chakra.stack
            (fun x ->
                x.spacing <- "0"
                x.overflow <- "hidden"
                x.flex <- "1")
            [
                Chakra.flex
                    (fun x ->
                        x.paddingLeft <- "9px"
                        x.paddingTop <- "1px"
                        x.marginLeft <- "1px"
                        x.borderBottomWidth <- "1px"
                        x.borderBottomColor <- "gray.16"
                        x.alignItems <- "center")
                    [
                        Chakra.icon
                            (fun x ->
                                x.``as`` <- input.Icon
                                x.marginRight <- "6px")
                            []
                        str input.Name

                        Chakra.spacer (fun _ -> ()) []

                        yield!
                            input.RightIcons
                            |> List.map
                                (fun icon ->
                                    match icon with
                                    | DockPanelIcon.Component cmp -> cmp
                                    | DockPanelIcon.Menu (title, icon, menu) ->
                                        Menu.Menu
                                            {|
                                                Tooltip = title
                                                Trigger =
                                                    TransparentIconButton.TransparentIconButton
                                                        {|
                                                            Props =
                                                                fun x ->
                                                                    x.``as`` <- Chakra.react.MenuButton
                                                                    x.fontSize <- "14px"
                                                                    x.icon <- icon
                                                        |}
                                                Body = menu
                                                MenuListProps = fun _ -> ()
                                            |})

                        Tooltip.wrap
                            (str "Hide")
                            [
                                TransparentIconButton.TransparentIconButton
                                    {|
                                        Props =
                                            fun x ->
                                                x.icon <- Icons.fa.FaMinus |> Icons.render
                                                x.onClick <- fun _ -> promise { setAtom None }
                                    |}
                            ]
                    ]

                Chakra.flex
                    (fun x ->
                        x.direction <- "column"
                        x.flex <- "1")
                    [
                        yield! input.children
                    ]
            ]
