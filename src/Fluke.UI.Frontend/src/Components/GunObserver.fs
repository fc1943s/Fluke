namespace Fluke.UI.Frontend.Components

open Feliz
open Feliz.UseListener
open Feliz.Recoil
open Fluke.UI.Frontend.Bindings
open Fluke.UI.Frontend.Hooks
open Fluke.UI.Frontend.State
open Fable.React
open Fable.Core


module GunObserver =

    [<ReactComponent>]
    let GunObserver () =
        let gun = Recoil.useValue Selectors.gun
        let gunNamespace = Recoil.useValue Selectors.gunNamespace
        //        let appKeys = Gun.gunHooks.useGunKeys Browser.Dom.window?SEA (fun () -> null) false
        let gunKeys, setGunKeys = Recoil.useState Atoms.gunKeys

        //        let gunState =
//            Gun.gunHooks.useGunState
//                (gunNamespace.ref.get ("fluke"))
//                {|
//                    appKeys = gunKeys
//                    sea = Browser.Dom.window?SEA
//                |}
//
//        printfn $"GunObserver. gunState={JS.JSON.stringify gunState}"
//        printfn "GunObserver. setted dom.gunState"
//        Browser.Dom.window?gunState <- gunState

        //        const [appKeys, setAppKeys] = useGunKeys(
//          sea,
//          () =>
//            JSON.parse(localStorage.getItem('existingKeysInLocalStorage')) || null,
//        );
//        const [user, isLoggedIn] = useGunKeyAuth(gun, appKeys);

        //        let setUsername = Recoil.useSetState Atoms.username

        let setSessionRestored = Recoil.useSetState Atoms.sessionRestored

        printfn "GunObserver.render: Constructor"


        React.useEffect (
            (fun () ->
                //                let recall = Browser.Dom.window.sessionStorage.getItem "recall"
//                printfn $"recall {recall}"
//
//                match recall with
//                | null
//                | "" -> setSessionRestored true
//                | _ -> ()
//
//                printfn "before recall"
//
//                try
//                    if true then
//                        gunNamespace.ref.recall (
//                            {| sessionStorage = true |},
//                            (fun ack ->
//                                match ack.put with
//                                | Some put -> setUsername (Some (UserInteraction.Username put.alias))
//                                | None -> printfn "Empty ack"
//
//                                setKeys (Some ack.sea)
//
//                                setSessionRestored true
//
//                                printfn "ACK:"
//                                Browser.Dom.console.log ack
//                                Browser.Dom.window?recallAck <- ack
//                                Dom.set "ack" ack)
//                        )
//                with ex -> printfn $"ERROR: {ex}"

                //                printfn "after recall"

                printfn "before newRecall"

                printfn $"gunKeys={JS.JSON.stringify gunKeys}"
                setSessionRestored true

                printfn "after newRecall"),
            [|
                box gunNamespace.``#``
                box gunKeys
            |]
        )

        React.useDisposableEffect (
            (fun disposed ->
                gun.``#``.on (
                    "auth",
                    (fun () ->
                        match disposed.current with
                        | false ->

                            match gunNamespace.``#``.is.alias with
                            | Some username ->
                                printfn $"GunObserver.render: .on(auth) effect. setUsername. username={username}"
                                setGunKeys gunNamespace.``#``._underscore_.sea

                            //                                gunState.put ({| username = username |} |> toPlainJsObj)
//                                |> Promise.start
                            //                                setUsername (Some (UserInteraction.Username username))
                            | None ->
                                printfn $"GunObserver.render: Auth occurred without username: {gunNamespace.``#``.is}"
                        | true -> ())
                )),
            [|
                box gun.``#``
                box gunNamespace.``#``
            |]
        )

        nothing
