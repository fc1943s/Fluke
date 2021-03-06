namespace Fluke.UI.Frontend.Bindings


#nowarn "40"


open System.Collections.Generic
open Fable.Extras
open Fluke.Shared.Domain.UserInteraction
open Fluke.UI.Frontend.Bindings
open Fable.Core.JsInterop
open Fable.Core
open System
open Fluke.Shared
open Microsoft.FSharp.Core.Operators
open Feliz


module Store =
    open Jotai

    let inline atom<'TValue> (atomPath, defaultValue: 'TValue) =
        jotai.atom (
            (fun () ->
                Profiling.addCount atomPath
                defaultValue)
                ()
        )
        |> registerAtom atomPath None
        |> fst

    let inline atomFamily<'TKey, 'TValue> (atomPath, defaultValueFn: 'TKey -> 'TValue) =
        jotaiUtils.atomFamily (fun param -> atom (atomPath, defaultValueFn param)) DeepEqual.compare

    let inline selector<'TValue>
        (
            atomPath,
            keyIdentifier,
            getFn: GetFn -> 'TValue,
            setFn: GetFn -> SetFn -> 'TValue -> unit
        ) =
        jotai.atom (
            (fun getter ->
                Profiling.addCount atomPath
                getFn getter),
            Some
                (fun getter setter value ->
                    Profiling.addCount $"{atomPath} set"
                    let newValue = value
                    //                        match jsTypeof value with
//                         | "function" -> (unbox value) () |> unbox
//                         | _ -> value
                    setFn getter setter newValue)
        )
        |> registerAtom atomPath keyIdentifier
        |> fst


    let inline readSelector<'TValue> (atomPath, getFn: GetFn -> 'TValue) =
        selector (
            atomPath,
            None,
            getFn,
            (fun _ _ _ ->
                Profiling.addCount $"{atomPath} set"
                failwith "readonly selector")
        )

    let inline readSelectorFamily<'TKey, 'TValue>
        (
            atomPath,
            getFn: 'TKey -> GetFn -> 'TValue
        ) : ('TKey -> Atom<'TValue>) =
        jotaiUtils.atomFamily (fun param -> readSelector (atomPath, getFn param)) DeepEqual.compare

    let inline value<'TValue> (getter: GetFn) (atom: Atom<'TValue>) : 'TValue = (getter (unbox atom)) :?> 'TValue

    let inline set<'TValue> (setter: SetFn) (atom: Atom<'TValue>) (value: 'TValue) = setter (atom |> box |> unbox) value

    let inline change<'TValue> (setter: SetFn) (atom: Atom<'TValue>) (value: 'TValue -> 'TValue) =
        setter (atom |> box |> unbox) value

    let inline selectAtom (atomPath: string, atom, selector) =
        //        readSelector (
//            atomPath,
//            fun getter ->
//                let value = value getter atom
//                Profiling.addCount $"{atomPath} :selectAtom"
//                selector value
//        )

        jotaiUtils.selectAtom
            atom
            (fun value ->
                Profiling.addCount $"{atomPath} :selectAtom"
                selector value)
            JS.undefined

    let inline selectAtomFamily (atomPath, atom, selector) =
        jotaiUtils.atomFamily (fun param -> selectAtom (atomPath, atom, selector param)) DeepEqual.compare

    let atomWithStorage (atomPath, defaultValue) =
        let internalAtom = jotaiUtils.atomWithStorage atomPath defaultValue

        jotai.atom (
            (fun getter -> value getter internalAtom),
            Some
                (fun _ setter argFn ->
                    let arg =
                        match jsTypeof argFn with
                        | "function" -> (argFn |> box |> unbox) () |> unbox
                        | _ -> argFn

                    set setter internalAtom arg)
        )
        |> registerAtom atomPath None
        |> fst


    module Atoms =
        let rec gunPeers = atomWithStorage ($"{nameof gunPeers}", ([||]: string []))
        let rec isTesting = atom ($"{nameof isTesting}", JS.deviceInfo.IsTesting)
        let rec username = atom ($"{nameof username}", (None: Username option))
        let rec gunKeys = atom ($"{nameof gunKeys}", Gun.GunKeys.Default)


    module Selectors =
        let rec gun =
            readSelector (
                $"{nameof gun}",
                (fun getter ->
                    let isTesting = value getter Atoms.isTesting
                    let gunPeers = value getter Atoms.gunPeers

                    let peers =
                        gunPeers
                        |> Array.filter (String.IsNullOrWhiteSpace >> not)

                    let gun =
                        if isTesting then
                            Gun.gun
                                {
                                    Gun.GunProps.peers = None
                                    Gun.GunProps.radisk = Some false
                                    Gun.GunProps.localStorage = Some false
                                    Gun.GunProps.multicast = None
                                }
                        else
                            Gun.gun
                                {
                                    Gun.GunProps.peers = Some peers
                                    Gun.GunProps.radisk = Some true
                                    Gun.GunProps.localStorage = Some false
                                    Gun.GunProps.multicast = None
                                }

                    printfn $"Gun selector. peers={peers}. gun={gun} returning..."

                    gun)
            )

        let rec gunNamespace =
            selectAtom (
                $"{nameof gunNamespace}",
                gun,
                fun gun ->
                    let user = gun.user ()

                    printfn $"gunNamespace selector. user.is={JS.JSON.stringify user.is} keys={user.__.sea}..."

                    user
            )

        let rec gunAtomNode =
            selectAtomFamily (
                $"{nameof gunAtomNode}",
                gunNamespace,
                (fun (username, atomPath) gunNamespace ->
                    match gunNamespace.is with
                    | Some { alias = Some username' } when username' = (username |> Username.Value) ->
                        let nodes =
                            atomPath
                            |> AtomPath.Value
                            |> String.split "/"
                            |> Array.toList

                        (Some (gunNamespace.get nodes.Head), nodes.Tail)
                        ||> List.fold
                                (fun result node ->
                                    result
                                    |> Option.map (fun result -> result.get node))
                    | _ ->
                        JS.log
                            (fun () ->
                                $"Invalid username.
                                                                                atomPath={atomPath}
                                                                                user.is={JS.JSON.stringify gunNamespace.is}")

                        None)
            )

    let inline gunAtomNodeFromAtomPath getter username atomPath =
        match username, atomPath with
        | Some username, Some atomPath ->
            match value getter (Selectors.gunAtomNode (username, atomPath)) with
            | Some gunAtomNode -> Some ($">> atomPath={atomPath} username={username}", gunAtomNode)
            | _ -> None
        | _ -> None

    let testKeysCache = Dictionary<string, Set<string>> ()

    let splitAtomPath atomPath =
        let matches =
            (JSe.RegExp @"(.*?)\/([\w-]{36})\/\w+.*?")
                .Match atomPath
            |> Option.ofObj
            |> Option.defaultValue Seq.empty
            |> Seq.toList

        match matches with
        | _match :: root :: guid :: _key -> Some (root, guid)
        | _ -> None


    // https://i.imgur.com/GB8trpT.png        :~ still trash
    let inline atomWithSync<'TKey, 'TValue> (atomPath, defaultValue: 'TValue, keyIdentifier: string list) =
        let mutable lastGunAtomNode = None
        let mutable lastValue = None
        let mutable lastGunValue = None
        let mutable lastAtomPath = None
        let mutable lastUserAtomId = None
        let mutable lastWrapperSet = None
        let mutable syncPaused = false

        let assignLastGunAtomNode getter atom =
            if lastAtomPath.IsNone then
                lastAtomPath <- queryAtomPath (AtomReference.Atom (unbox atom))

            JS.log
                (fun () ->
                    match lastAtomPath with
                    | Some (AtomPath atomPath) when atomPath.Contains "devicePing" |> not ->
                        $"assignLastGunAtomNode atom={atom} lastAtomPath={atomPath}"
                    | _ -> null)

            let username = value getter Atoms.username
            lastGunAtomNode <- gunAtomNodeFromAtomPath getter username lastAtomPath
            username


        let internalAtom = jotaiUtils.atomFamily (fun _username -> jotai.atom defaultValue) DeepEqual.compare

        let gunNodePath = Gun.getGunNodePath atomPath keyIdentifier

        Profiling.addCount $"{gunNodePath} constructor"

        let baseInfo () =
            $"""gunNodePath={gunNodePath}
atomPath={atomPath}
keyIdentifier={keyIdentifier}
lastValue={lastValue}
lastGunValue={lastGunValue}
lastGunAtomNode={lastGunAtomNode}
lastAtomPath={lastAtomPath}
lastUserAtomId={lastUserAtomId} """


        JS.log
            (fun () ->
                $"atomFamily constructor
                {baseInfo ()}")

        let mutable lastSubscription = None

        let unsubscribe () =
            match lastSubscription with
            | Some ticks when DateTime.ticksDiff ticks < 1000. ->
                JS.log
                    (fun () ->
                        $"[wrapper.off()]
                                                    {baseInfo ()}
                                                    skipping unsubscribe. jotai resubscribe glitch.")
            | Some _ ->
                match lastGunAtomNode with
                | Some (key, gunAtomNode) ->

                    Profiling.addCount $"{gunNodePath} unsubscribe"

                    JS.log
                        (fun () ->
                            $"[atomFamily.unsubscribe()]
                                {key}
                                {baseInfo ()} (######## actually skipped) ")

                    if false then
                        gunAtomNode.off () |> ignore
                        lastSubscription <- None
                | None ->
                    JS.log
                        (fun () ->
                            $"[wrapper.off()]
                                {baseInfo ()}
                                skipping unsubscribe, no gun atom node.")
            | None ->
                JS.log
                    (fun () ->
                        $"[wrapper.off()]
                                {baseInfo ()}
                                skipping unsubscribe. no last subscription found.")

        let setInternalFromGun gunAtomNode setAtom =
            JS.debounce
                (fun (ticks, data) ->
                    promise {
                        try
                            let! newValue =
                                match box data with
                                | null -> unbox null |> Promise.lift
                                | _ -> Gun.userDecode<'TValue> gunAtomNode data

                            lastGunValue <- newValue

                            match syncPaused, lastValue with
                            | true, _ ->
                                JS.log
                                    (fun () ->
                                        if (string newValue).StartsWith "Ping " then
                                            null
                                        else
                                            $"gun.on() value. skipping. Sync paused.
                                                    jsTypeof-newValue={jsTypeof newValue}
                                                    newValue={newValue}
                                                    lastValue={lastValue}
                                                    ticks={ticks}
                                                    {baseInfo ()} ")
                            | _, Some (lastValueTicks, lastValue) when
                                lastValueTicks > ticks
                                || lastValue |> DeepEqual.compare (unbox newValue)
                                || (unbox lastValue = null && unbox newValue = null)
                                ->

                                Profiling.addCount $"{gunNodePath} on() skip"

                                JS.log
                                    (fun () ->
                                        if (string newValue).StartsWith "Ping " then
                                            null
                                        else
                                            $"gun.on() value. skipping.
                                                    jsTypeof-newValue={jsTypeof newValue}
                                                    newValue={newValue}
                                                    lastValue={lastValue}
                                                    ticks={ticks}
                                                    {baseInfo ()} ")
                            | _ ->
                                Profiling.addCount $"{gunNodePath} on() assign"

                                JS.log
                                    (fun () ->
                                        let _lastValue =
                                            match unbox lastValue with
                                            | Some (_, b) -> b
                                            | _ -> null

                                        if string _lastValue = string newValue then
                                            Browser.Dom.console.error
                                                $"should have skipped assign
                                        lastValue={lastValue}
                                        typeof _lastValue={jsTypeof _lastValue}
                                        newValue={newValue}
                                        typeof newValue={jsTypeof newValue}
                                        ticks={ticks}
                                        {baseInfo ()} "

                                        if (string newValue).StartsWith "Ping " then
                                            null
                                        else
                                            $"gun.on() value. triggering.
                                lastValue={lastValue}
                                typeof _lastValue={jsTypeof _lastValue}
                                newValue={newValue}
                                typeof newValue={jsTypeof newValue}
                                ticks={ticks}
                                {baseInfo ()} ")

                                //                        Browser.Dom.window?atomPath <- atomPath
//                        Browser.Dom.window?lastValue <- _lastValue
//                        Browser.Dom.window?newValue <- newValue
//                        Browser.Dom.window?deepEqual <- DeepEqual.compare

                                // setAtom internalAtom

                                setAtom newValue
                        with
                        | ex ->
                            Browser.Dom.console.error ("[exception1]", ex)
                            lastSubscription <- None
                    })
                500

        let subscribe =
            JS.debounce
                (fun setAtom ->
                    lastWrapperSet <- Some setAtom

                    match lastGunAtomNode, lastSubscription with
                    | _, Some _ ->
                        JS.log
                            (fun () ->
                                $"[wrapper.on() subscribe]
                                {baseInfo ()}
                             skipping subscribe, lastSubscription is set.")
                    | Some (key, gunAtomNode), None ->

                        Profiling.addCount $"{gunNodePath} subscribe"

                        JS.log
                            (fun () ->
                                $"[wrapper.on() subscribe] batch subscribing.
                        atomPath={atomPath} {key}")

                        //                    gunAtomNode.off () |> ignore

                        Gun.batchSubscribe
                            {|
                                GunAtomNode = gunAtomNode
                                Fn = setInternalFromGun gunAtomNode setAtom
                            |}

                        //                        Gun.subscribe
//                            gunAtomNode
//                            (fun data ->
//                                setInternalFromGun gunAtomNode setAtom (DateTime.Now.Ticks, data)
//                                |> Promise.start)

                        lastSubscription <- Some DateTime.Now.Ticks

                    | None, _ ->
                        JS.log
                            (fun () ->
                                $"[wrapper.on() subscribe]
                                {baseInfo ()}
                             skipping subscribe, no gun atom node."))
                100

        let debounceGunPut =
            JS.debounce
                (fun newValue ->
                    promise {
                        JS.log
                            (fun () ->
                                if (string newValue).StartsWith "Ping " then
                                    null
                                else
                                    "atomFamily.wrapper.set() debounceGunPut promise. #1")

                        try
                            match lastGunAtomNode with
                            | Some (key, gunAtomNode) ->
                                JS.log
                                    (fun () ->
                                        if (string newValue).StartsWith "Ping " then
                                            null
                                        else
                                            $"atomFamily.wrapper.set() debounceGunPut promise. #2 before encode {key}")

                                let! newValueJson =
                                    if newValue |> JS.ofNonEmptyObj |> Option.isNone then
                                        null |> Promise.lift
                                    else
                                        Gun.userEncode<'TValue> gunAtomNode newValue

                                JS.log
                                    (fun () ->
                                        if (string newValue).StartsWith "Ping " then
                                            null
                                        else
                                            $"atomFamily.wrapper.set() debounceGunPut promise. #3.
                                            before put {key} newValue={newValue}")

                                if lastGunValue.IsNone
                                   || lastGunValue
                                      |> DeepEqual.compare (unbox newValue)
                                      |> not
                                   || unbox newValue = null then

                                    let! putResult = Gun.put gunAtomNode newValueJson

                                    if putResult then
                                        lastGunValue <- Some newValue

                                        JS.log
                                            (fun () ->
                                                if (string newValue).StartsWith "Ping " then
                                                    null
                                                else
                                                    $"atomFamily.wrapper.set() debounceGunPut promise result.
                                                       newValue={newValue}
                                                       {key}
                                                       {baseInfo ()} ")
                                    else
                                        Browser.Dom.window?lastPutResult <- putResult

                                        match JS.window id with
                                        | Some window ->
                                            if window?Cypress = null then
                                                Browser.Dom.console.error
                                                    $"atomFamily.wrapper.set() debounceGunPut promise put error.
                                                         newValue={newValue} putResult={putResult}
                                                           {key}
                                                        {baseInfo ()}"
                                        | None -> ()
                                else
                                    JS.log
                                        (fun () ->
                                            if (string newValue).StartsWith "Ping " then
                                                null
                                            else
                                                $"atomFamily.wrapper.set() debounceGunPut promise.
                                                   put skipped
                                                   newValue[{newValue}]==lastGunValue[]
                                                   {key}
                                                   {baseInfo ()} ")
                            | None ->
                                JS.log
                                    (fun () ->
                                        $"[gunEffect.debounceGunPut promise]
                                        skipping gun put. no gun atom node.
                                        {baseInfo ()} ")
                        with
                        | ex -> Browser.Dom.console.error ("[exception2]", ex)

                        syncPaused <- false
                    }
                    |> Promise.start)
                100

        let rec wrapper =
            selector (
                atomPath,
                (Some keyIdentifier),
                (fun getter ->
                    let username = assignLastGunAtomNode getter wrapper
                    let userAtom = internalAtom username

                    let result =
                        value getter userAtom
                        |> Option.ofObjUnbox
                        |> Option.defaultValue defaultValue

                    Profiling.addCount $"{gunNodePath} get"

                    JS.log
                        (fun () ->
                            if (string result
                                |> Option.ofObjUnbox
                                |> Option.defaultValue "")
                                .StartsWith "Ping " then
                                null
                            else
                                $"atomFamily.wrapper.get()
                                wrapper={wrapper}
                                userAtom={userAtom}
                                result={result}
                                {baseInfo ()} ")

                    let userAtomId = Some (userAtom.toString ())

                    if userAtomId <> lastUserAtomId then
                        lastUserAtomId <- userAtomId

                        match lastWrapperSet, lastSubscription with
                        | Some lastWrapperSet, None ->
                            JS.log
                                (fun () ->
                                    $"atomFamily.wrapper.get() subscribing
                                wrapper={wrapper}
                                userAtom={userAtom}
                                {baseInfo ()} ")

                            subscribe lastWrapperSet
                        | _ ->
                            JS.log
                                (fun () ->
                                    $"atomFamily.wrapper.get() skipping subscribe
                                wrapper={wrapper}
                                userAtom={userAtom}
                                {baseInfo ()} ")

                    lastValue <- Some (DateTime.Now.Ticks, result)

                    result),
                (fun getter setter newValueFn ->
                    let username = assignLastGunAtomNode getter wrapper
                    let userAtom = internalAtom username

                    Profiling.addCount $"{gunNodePath} set"

                    set
                        setter
                        userAtom
                        (unbox
                            (fun oldValue ->
                                let newValue =
                                    match jsTypeof newValueFn with
                                    | "function" -> (unbox newValueFn) oldValue |> unbox
                                    | _ -> newValueFn

                                match lastGunValue with
                                | Some lastGunValue when lastGunValue |> DeepEqual.compare newValue ->
                                    JS.log
                                        (fun () ->
                                            if (string newValue).StartsWith "Ping " then
                                                null
                                            else
                                                $"atomFamily.wrapper.set() SKIPPED
                                                    wrapper={wrapper}
                                                    userAtom={userAtom}
                                                    jsTypeof-newValue={jsTypeof newValue}
                                                    oldValue={oldValue}
                                                    newValue={newValue}
                                                    {baseInfo ()} ")
                                | _ ->
                                    if true
                                       || oldValue |> DeepEqual.compare newValue |> not
                                       || (lastValue.IsNone
                                           && newValue |> DeepEqual.compare defaultValue) then

                                        JS.log
                                            (fun () ->
                                                if (string newValue).StartsWith "Ping " then
                                                    null
                                                else
                                                    $"atomFamily.wrapper.set()
                                                    wrapper={wrapper}
                                                    userAtom={userAtom}
                                                    jsTypeof-newValue={jsTypeof newValue}
                                                    oldValue={oldValue}
                                                    newValue={newValue}
                                                    {baseInfo ()}
                                                    $$ (should abort set? oldValue==newValue==lastValue/defaultValue)
                                                    ")

                                        syncPaused <- true
                                        debounceGunPut newValue

                                lastValue <- Some (DateTime.Now.Ticks, newValue)

                                //                                JS.log
//                                    (fun () ->
//                                        if (string newValue).StartsWith "Ping " then
//                                            null
//                                        else
//                                            "atomFamily.wrapper.set() ##### lastValue setted. returning ##### ")

                                if JS.jestWorkerId then
                                    match splitAtomPath gunNodePath with
                                    | Some (root, guid) ->
                                        let newSet =
                                            match testKeysCache.TryGetValue root with
                                            | true, guids -> guids |> Set.add guid
                                            | _ -> Set.singleton guid

                                        testKeysCache.[root] <- newSet
                                    | None -> ()

                                newValue)))
            )

        if keyIdentifier
           <> [
               string Guid.Empty
           ] then
            wrapper?onMount <- fun setAtom ->
                                   subscribe setAtom
                                   fun () -> unsubscribe ()

        wrapper

    let inline asyncSelector<'TValue>
        (
            atomPath,
            keyIdentifier,
            getFn: GetFn -> JS.Promise<'TValue>,
            setFn: GetFn -> SetFn -> 'TValue -> JS.Promise<unit>
        ) =
        jotai.atom (
            (fun getter ->
                promise {
                    Profiling.addCount $"{atomPath}"
                    let a = getFn getter
                    return! a
                }),
            Some
                (fun getter setter newValue ->
                    promise {
                        Profiling.addCount $"{atomPath} set"
                        do! setFn getter setter newValue
                    })
        )
        |> registerAtom atomPath keyIdentifier
        |> fst

    let inline asyncReadSelector<'TValue> (atomPath, getFn: GetFn -> JS.Promise<'TValue>) =
        asyncSelector (
            atomPath,
            None,
            getFn,
            (fun _ _ _newValue -> promise { failwith $"readonly selector {atomPath}" })
        )

    let inline selectAtomSyncKeys
        (
            atomPath: string,
            atomFamily: 'TKey -> Atom<_>,
            key: 'TKey,
            onFormat: string -> 'TKey
        ) : Atom<Atom<'TKey> []> =
        Profiling.addCount $"{atomPath} :selectAtomSyncKeys"

        let atom = atomFamily key
        JS.log (fun () -> "@@ #1")

        let mutable lastGunAtomNode = None
        let mutable lastAtomPath = None

        let assignLastGunAtomNode getter =
            if lastAtomPath.IsNone then
                lastAtomPath <- queryAtomPath (AtomReference.Atom (unbox atom))

            JS.log (fun () -> $"@@ assignLastGunAtomNode atom={atom} lastAtomPath={lastAtomPath}")

            let username = value getter Atoms.username

            lastGunAtomNode <-
                gunAtomNodeFromAtomPath getter username lastAtomPath
                |> Option.map (fun (key, node) -> key, node.back().back ())

            username

        let internalAtom = jotaiUtils.atomFamily (fun _username -> jotai.atom [||]) DeepEqual.compare

        let keyIdentifier = []
        let gunNodePath = Gun.getGunNodePath atomPath keyIdentifier

        Profiling.addCount $"@@ {gunNodePath} constructor"

        let baseInfo () =
            $"""@@ gunNodePath={gunNodePath}
                atomPath={atomPath}
                keyIdentifier={keyIdentifier}
                lastGunAtomNode={lastGunAtomNode}
                lastAtomPath={lastAtomPath}
                """

        JS.log
            (fun () ->
                $"@@ atomFamily constructor
                {baseInfo ()}")

        let rec wrapper =
            selector (
                atomPath,
                None,
                (fun getter ->
                    let username = assignLastGunAtomNode getter
                    let userAtom = internalAtom username

                    Profiling.addCount $"{gunNodePath} get"


                    if not JS.jestWorkerId then

                        let result = value getter userAtom

                        JS.log
                            (fun () ->
                                $"@@ atomFamily.wrapper.get()
                                    wrapper={wrapper}
                                    userAtom={userAtom}
                                    result={result}
                                    {baseInfo ()} ")

                        result
                    else
                        match lastAtomPath with
                        | Some (AtomPath atomPath) ->
                            match splitAtomPath atomPath with
                            | Some (root, _guid) ->
                                match testKeysCache.TryGetValue root with
                                | true, guids -> guids |> Set.toArray |> Array.map onFormat
                                | _ -> [||]
                            | None -> [||]
                        | None -> [||]),
                (fun getter setter newValueFn ->
                    let username = assignLastGunAtomNode getter
                    let userAtom = internalAtom username

                    set
                        setter
                        userAtom
                        (unbox
                            (fun oldValue ->
                                let newValue =
                                    match jsTypeof newValueFn with
                                    | "function" -> (unbox newValueFn) oldValue |> unbox
                                    | _ -> newValueFn

                                JS.log (fun () -> $"@@ newValue={newValue} newValueFn={newValueFn}")

                                newValue)))
            )

        let mutable lastSubscription = None

        let debouncedSet setAtom =
            JS.debounce
                (fun data ->
                    //                        Browser.Dom.window?lastData <- data

                    let result =
                        JS.Constructors.Object.entries data
                        |> unbox<(string * obj) []>
                        |> Array.filter
                            (fun (guid, value) ->
                                guid.Length = 36
                                && guid <> string Guid.Empty
                                && value <> null)
                        |> Array.map (fst >> onFormat)

                    JS.log
                        (fun () ->
                            $"@@ [atomKeys debouncedSet gun.on() data]
                                                   atomPath={atomPath}
                                                   len={result.Length}
                                                   {key} ")

                    setAtom result)
                750

        let subscribe =
            JS.debounce
                (fun setAtom ->
                    JS.log (fun () -> "@@ #3")

                    match lastGunAtomNode, lastSubscription with
                    | _, Some _ ->
                        JS.log
                            (fun () ->
                                $"@@ [atomKeys gun.on() subscribing]
                                                   {baseInfo ()}
                                                skipping subscribe, lastSubscription is set.")
                    | Some (key, gunAtomNode), None ->
                        Profiling.addCount $"@@ {gunNodePath} subscribe"
                        JS.log (fun () -> $"@@ [atomKeys gun.on() subscribing] atomPath={atomPath} {key}")

                        let setData = debouncedSet setAtom

                        gunAtomNode.on (fun data _key -> setData data)

                        lastSubscription <- Some DateTime.Now.Ticks
                    | None, _ ->
                        JS.log
                            (fun () ->
                                $"@@ [atomKeys gun.on() subscribing]
                                                   {baseInfo ()}
                                                skipping subscribe, no gun atom node."))
                100

        let unsubscribe () =
            match lastSubscription with
            | Some ticks when DateTime.ticksDiff ticks < 1000. ->
                JS.log
                    (fun () ->
                        $"@@ [atomKeys gun.off()]
                                                    {baseInfo ()}
                                                    skipping unsubscribe. jotai resubscribe glitch.")
            | Some _ ->
                match lastGunAtomNode with
                | Some (key, _gunAtomNode) ->

                    Profiling.addCount $"@@ {gunNodePath} unsubscribe"

                    JS.log
                        (fun () ->
                            $"@@  [atomFamily.unsubscribe()]
                               {key}
                               {baseInfo ()}
                               ############ (actually skipped)
                               ")

                //                    gunAtomNode.off () |> ignore
//                    lastSubscription <- None
                | None ->
                    JS.log
                        (fun () ->
                            $"@@  [atomKeys gun.off()]
                                                               {baseInfo ()}
                                                               skipping unsubscribe, no gun atom node.")
            | None ->
                JS.log
                    (fun () ->
                        $"[atomKeys gun.off()]
                                {baseInfo ()}
                                skipping unsubscribe. no last subscription found.")

        wrapper?onMount <- fun setAtom ->
                               subscribe setAtom
                               fun _ -> unsubscribe ()

        jotaiUtils.splitAtom wrapper


    let inline atomFamilyWithSync<'TKey, 'TValue>
        (
            atomPath,
            defaultValueFn: 'TKey -> 'TValue,
            persist: 'TKey -> string list
        ) =
        jotaiUtils.atomFamily
            (fun param -> atomWithSync (atomPath, defaultValueFn param, persist param))
            DeepEqual.compare

    let inline atomWithStorageSync<'TKey, 'TValue> (atomPath, defaultValue) =
        let storageAtom = atomWithStorage (atomPath, defaultValue)
        let syncAtom = atomWithSync<'TKey, 'TValue> (atomPath, defaultValue, [])

        let mutable lastSetAtom = None
        let mutable lastValue = None

        let rec wrapper =
            selector (
                atomPath,
                None,
                (fun getter ->
                    match value getter syncAtom, value getter storageAtom with
                    | syncValue, storageValue when
                        syncValue |> DeepEqual.compare defaultValue
                        && (storageValue |> DeepEqual.compare defaultValue
                            || (value getter Atoms.username).IsNone
                            || lastValue.IsNone)
                        ->
                        value getter storageAtom
                    | syncValue, _ ->
                        match lastSetAtom with
                        | Some lastSetAtom when
                            lastValue.IsNone
                            || lastValue
                               |> DeepEqual.compare (Some syncValue)
                               |> not
                            ->
                            lastValue <- Some syncValue
                            lastSetAtom syncValue
                        | _ -> ()

                        syncValue),
                (fun _get setter newValue ->
                    if lastValue.IsNone
                       || lastValue
                          |> DeepEqual.compare (Some newValue)
                          |> not then
                        lastValue <- Some newValue
                        set setter syncAtom newValue

                    set setter storageAtom newValue)
            )

        wrapper?onMount <- fun setAtom ->
                               lastSetAtom <- setAtom
                               fun () -> lastSetAtom <- None

        wrapper


    let tempValue =
        let rec tempValue =
            atomFamilyWithSync (
                $"{nameof tempValue}",
                (fun (_guid: Guid) -> null: string),
                (fun (guid: Guid) ->
                    [
                        string guid
                    ])
            )

        jotaiUtils.atomFamily
            (fun (AtomPath atomPath) ->

                let guidHash = Crypto.getTextGuidHash atomPath
                let atom = tempValue guidHash

                JS.log (fun () -> $"tempValueWrapper constructor. atomPath={atomPath} guidHash={guidHash}")

                let wrapper =
                    jotai.atom (
                        (fun getter ->
                            let value = value getter atom
                            Profiling.addCount $"{atomPath} tempValue set"

                            JS.log
                                (fun () ->
                                    $"tempValueWrapper.get(). atomPath={atomPath} guidHash={guidHash} value={value}")

                            match value with
                            | null -> null
                            | _ ->
                                match Json.decode<string * string option> value with
                                | _, Some value -> value
                                | _ -> null),
                        Some
                            (fun _ setter newValue ->
                                Profiling.addCount $"{atomPath} tempValue set"

                                JS.log
                                    (fun () ->
                                        $"tempValueWrapper.set(). atomPath={atomPath}
                                        guidHash={guidHash} newValue={newValue}")

                                let newValue = Json.encode (atomPath, newValue |> Option.ofObj)

                                JS.log (fun () -> $"tempValueWrapper.set(). newValue2={newValue}")

                                set setter atom (newValue |> box |> unbox))
                    )

                wrapper)
            DeepEqual.compare

    let inline useValue atom = jotaiUtils.useAtomValue atom

    let shadowedCallbackFn (fn, deps) =
        (emitJsExpr (React.useCallback, fn, deps) "$0($1,$2)")

    let useCallback (fn: GetFn -> SetFn -> 'a -> JS.Promise<'c>, deps: obj []) : ('a -> JS.Promise<'c>) =

        let fnCallback = React.useCallbackRef (fun (getter, setter, arg) -> fn getter setter arg)

        let fnCallback =
            shadowedCallbackFn (
                fnCallback,
                Array.concat [
                    deps
                    [|
                        box fnCallback
                    |]
                ]
            )

        let atom =
            React.useMemo (
                (fun () ->
                    jotai.atom (
                        unbox null,
                        Some
                            (fun getter setter (arg, resolve, err) ->
                                try
                                    resolve (fnCallback (getter, setter, arg))
                                with
                                | ex ->
                                    printfn $"atomCallback fn error: {ex}"
                                    err ex

                                ())
                    )),
                [|
                    box fnCallback
                |]
            )

        let _value, setValue = jotai.useAtom atom

        let useAtomCallback =
            React.useCallback (
                (fun arg ->
                    Promise.create (fun resolve err -> setValue (arg, resolve, err))
                    |> Promise.bind id),
                [|
                    box setValue
                |]
            )

        useAtomCallback

    let inline useCallbacks () =
        useCallback ((fun getter setter () -> promise { return (getter, setter) }), [||])


    let inline useState atom = jotai.useAtom atom

    let inline useSetState atom = jotaiUtils.useUpdateAtom atom

    //    let inline useSetStatePrev<'T> atom =
//        let setter = jotaiUtils.useUpdateAtom<'T> atom
//        fun (value: 'T -> 'T) -> setter (unbox value)

    let provider = jotai.provider

    type GetFn = Jotai.GetFn
    type SetFn = Jotai.SetFn
    type AtomReference<'T> = Jotai.AtomReference<'T>
    type Atom<'T> = Jotai.Atom<'T>
    let emptyArrayAtom = jotai.atom<obj []> [||]

    let waitForAll<'T> (atoms: Atom<'T> []) =
        match atoms with
        | [||] -> unbox emptyArrayAtom
        | _ -> jotaiUtils.waitForAll atoms


    let inline selectorFamily<'TKey, 'TValue>
        (
            atomPath,
            getFn: 'TKey -> GetFn -> 'TValue,
            setFn: 'TKey -> GetFn -> SetFn -> 'TValue -> unit
        ) =
        jotaiUtils.atomFamily (fun param -> selector (atomPath, None, getFn param, setFn param)) DeepEqual.compare


    let inline asyncSelectorFamily<'TKey, 'TValue>
        (
            atomPath,
            getFn: 'TKey -> GetFn -> JS.Promise<'TValue>,
            setFn: 'TKey -> GetFn -> SetFn -> 'TValue -> JS.Promise<unit>
        ) =
        jotaiUtils.atomFamily
            (fun param ->
                asyncSelector (
                    (atomPath,
                     None,
                     (getFn param),
                     (fun getter setter newValue -> promise { do! setFn param getter setter newValue }))
                ))
            DeepEqual.compare

    let inline asyncReadSelectorFamily<'TKey, 'TValue> (atomPath, getFn: 'TKey -> GetFn -> JS.Promise<'TValue>) =
        asyncSelectorFamily (
            atomPath,
            getFn,
            (fun _key _ _ _newValue -> promise { failwith $"readonly selector family {atomPath}" })
        )



    [<RequireQualifiedAccess>]
    type InputScope<'TValue> =
        | Current
        | Temp of Gun.Serializer<'TValue>

    and InputScope<'TValue> with
        static member inline AtomScope<'TValue> (inputScope: InputScope<'TValue> option) =
            match inputScope with
            | Some (InputScope.Temp _) -> AtomScope.Temp
            | _ -> AtomScope.Current

    and [<RequireQualifiedAccess>] AtomScope =
        | Current
        | Temp

    type InputAtom<'T> = InputAtom of atomPath: AtomReference<'T>

    type AtomField<'TValue67> =
        {
            Current: Jotai.Atom<'TValue67> option
            Temp: Jotai.Atom<string> option
        }

    let emptyAtom = jotai.atom<obj> null

    let inline getAtomField (atom: InputAtom<'TValue> option) (inputScope: AtomScope) =
        match atom with
        | Some (InputAtom atomPath) ->
            {
                Current =
                    match atomPath with
                    | AtomReference.Atom atom -> Some atom
                    | _ -> Some (unbox emptyAtom)
                Temp =
                    //                    JS.log
//                        (fun () -> $"getAtomField atomPath={atomPath} queryAtomPath atomPath={queryAtomPath atomPath}")

                    match queryAtomPath atomPath, inputScope with
                    | Some atomPath, AtomScope.Temp -> Some (tempValue atomPath)
                    | _ -> None
            }
        | _ -> { Current = None; Temp = None }


    let inline setTempValue<'TValue9, 'TKey> (setter: Jotai.SetFn, atom: Jotai.Atom<'TValue9>, value: 'TValue9) =
        let atomField = getAtomField (Some (InputAtom (AtomReference.Atom atom))) AtomScope.Temp

        match atomField.Temp with
        | Some atom -> set setter atom (value |> Json.encode<'TValue9>)
        | _ -> ()

    let inline scopedSet<'TValue10, 'TKey>
        (setter: Jotai.SetFn)
        (atomScope: AtomScope)
        (atom: 'TKey -> Jotai.Atom<'TValue10>, key: 'TKey, value: 'TValue10)
        =
        match atomScope with
        | AtomScope.Current -> set setter (atom key) value
        | AtomScope.Temp -> setTempValue<'TValue10, 'TKey> (setter, atom key, value)

    let inline resetTempValue<'TValue8, 'TKey> (setter: Jotai.SetFn) (atom: Jotai.Atom<'TValue8>) =
        let atomField = getAtomField (Some (InputAtom (AtomReference.Atom atom))) AtomScope.Temp

        match atomField.Temp with
        | Some atom -> set setter atom null
        | _ -> ()

    let rec ___emptyTempAtom = nameof ___emptyTempAtom

    let inline getTempValue<'TValue11, 'TKey> getter (atom: Jotai.Atom<'TValue11>) =
        let atomField = getAtomField (Some (InputAtom (AtomReference.Atom atom))) AtomScope.Temp

        match atomField.Temp with
        | Some tempAtom ->
            let result = value getter tempAtom

            match result with
            | result when result = ___emptyTempAtom -> unbox null
            | null -> value getter atom
            | _ -> Json.decode<'TValue11> result
        | _ -> value getter atom

    let deleteRoot getter atom =
        promise {
            let username = value getter Atoms.username
            let atomPath = queryAtomPath (AtomReference.Atom atom)
            let gunAtomNode = gunAtomNodeFromAtomPath getter username atomPath

            match gunAtomNode with
            | Some (_key, gunAtomNode) ->
                let! _putResult = Gun.put (gunAtomNode.back ()) (unbox null)
                ()
            | None -> ()
        }

    module Hooks =
        let useStateOption (atom: Jotai.Atom<'TValue5> option) =
            let flatAtom =
                React.useMemo (
                    (fun () ->
                        match atom with
                        | Some atom -> atom
                        | None -> emptyAtom :?> Jotai.Atom<'TValue5>),
                    [|
                        box atom
                    |]
                )

            let value, setValue = jotai.useAtom flatAtom

            React.useMemo (
                (fun () ->
                    (if atom.IsNone then None else Some value), (if atom.IsNone then (fun _ -> ()) else setValue)),
                [|
                    box atom
                    box value
                    box setValue
                |]
            )

        let useTempAtom<'TValue7> (atom: InputAtom<'TValue7> option) (inputScope: InputScope<'TValue7> option) =
            let atomField =
                React.useMemo (
                    (fun () -> getAtomField atom (InputScope.AtomScope inputScope)),
                    [|
                        box atom
                        box inputScope
                    |]
                )

            let currentValue, setCurrentValue = useStateOption atomField.Current
            let tempValue, setTempValue = useStateOption atomField.Temp

            React.useMemo (
                (fun () ->
                    let defaultJsonEncode, _defaultJsonDecode = unbox Gun.defaultSerializer

                    let newTempValue =
                        match inputScope, tempValue |> Option.defaultValue null with
                        | _, tempValue when tempValue = ___emptyTempAtom -> unbox null
                        | _, null -> currentValue |> Option.defaultValue (unbox null)
                        | Some (InputScope.Temp (_, jsonDecode)), tempValue ->
                            try
                                JS.log
                                    (fun () ->
                                        $"useTempAtom
                                currentValue={currentValue}
                                atom={atom}
                                tempValue={tempValue}")

                                jsonDecode tempValue
                            with
                            | ex ->
                                printfn $"Error decoding tempValue={tempValue} ex={ex}"

                                currentValue
                                |> Option.defaultValue (unbox tempValue)
                        | _ ->
                            currentValue
                            |> Option.defaultValue (unbox tempValue)

                    let setTempValue =
                        if atom.IsSome then
                            (fun newValue ->
                                setTempValue (
                                    match box newValue with
                                    | null -> ___emptyTempAtom
                                    | _ ->
                                        match inputScope with
                                        | Some (InputScope.Temp (jsonEncode, _)) -> jsonEncode newValue
                                        | _ -> defaultJsonEncode newValue
                                ))
                        else
                            (fun _ -> ())

                    let setCurrentValue = if atom.IsSome then setCurrentValue else (fun _ -> ())

                    {|
                        AtomField = atomField
                        Value =
                            match inputScope with
                            | Some (InputScope.Temp _) -> newTempValue
                            | _ -> currentValue |> Option.defaultValue (unbox null)
                        SetValue =
                            match inputScope with
                            | Some (InputScope.Temp _) -> setTempValue
                            | _ -> setCurrentValue
                        CurrentValue = currentValue |> Option.defaultValue (unbox null)
                        SetCurrentValue = setCurrentValue
                        TempValue = newTempValue
                        SetTempValue = setTempValue
                    |}),
                [|
                    box inputScope
                    box atom
                    box atomField
                    box currentValue
                    box tempValue
                    box setCurrentValue
                    box setTempValue
                |]
            )
