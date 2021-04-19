namespace Fluke.UI.Frontend.Components

open Feliz
open Feliz.Recoil
open Feliz.UseListener
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Components
open Fluke.UI.Frontend.Bindings


module Content =

    [<ReactComponent>]
    let Content () =
        Profiling.addTimestamp "mainComponent.render"
        let username = Recoil.useValue Recoil.Atoms.username
        let sessionRestored = Recoil.useValue Recoil.Atoms.sessionRestored
        let initialPeerSkipped = Recoil.useValue Recoil.Atoms.initialPeerSkipped
        let gunPeers = Recoil.useValue Recoil.Selectors.gunPeers
        let deviceInfo = Recoil.useValue Recoil.Selectors.deviceInfo

        Chakra.flex
            (fun x ->
                x.minHeight <- "100vh"
                x.height <- if deviceInfo.IsExtension then "590px" else null
                x.width <- if deviceInfo.IsExtension then "790px" else null)
            [
                match sessionRestored with
                | false -> LoadingScreen.LoadingScreen ()
                | true ->
                    match username with
                    | Some username ->
                        React.suspense (
                            [
                                SessionDataLoader.SessionDataLoader {| Username = username |}
                                SoundPlayer.SoundPlayer {| Username = username |}

                                Chakra.stack
                                    (fun x ->
                                        x.spacing <- "0"
                                        x.flex <- 1)
                                    [
                                        TopBar.TopBar ()
                                        HomeScreen.HomeScreen
                                            {|
                                                Username = username
                                                Props = JS.newObj (fun x -> x.flex <- 1)
                                            |}
                                        StatusBar.StatusBar {| Username = username |}
                                    ]
                            ],
                            LoadingScreen.LoadingScreen ()
                        )

                    | None ->
                        match gunPeers, initialPeerSkipped with
                        | [||], false -> InitialPeers.InitialPeers ()
                        | _ -> LoginScreen.LoginScreen ()
            ]
