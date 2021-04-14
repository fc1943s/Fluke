namespace Fluke.UI.Frontend.Components

open Fable.React
open Feliz
open System
open Feliz.Recoil
open Fluke.Shared.Domain
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Bindings
open Fable.DateFunctions
open Fable.Core.JsInterop
open Fable.Core


module DatabaseForm =
    open State
    open Model

    module DatabaseAccessIndicator =
        [<ReactComponent>]
        let DatabaseAccessIndicator () =
            Chakra.stack
                {|
                    direction = "row"
                    spacing = "15px"
                |}
                [
                    Chakra.stack
                        {|
                            direction = "row"
                            spacing = "4px"
                            align = "center"
                        |}
                        [
                            Chakra.circle
                                {|
                                    width = "10px"
                                    height = "10px"
                                    backgroundColor = "#0f0"
                                |}
                                []

                            Chakra.box
                                {|  |}
                                [
                                    str "Private"
                                ]

                        ]
                    Chakra.iconButton
                        {|
                            icon = Icons.bsThreeDots ()
                            disabled = true
                            width = "22px"
                            height = "15px"
                            onClick = fun () -> ()
                        |}
                        []

                ]

    [<ReactComponent>]
    let DatabaseForm
        (input: {| Username: UserInteraction.Username
                   DatabaseId: State.DatabaseId
                   OnSave: unit -> JS.Promise<unit> |})
        =
        let onSave =
            Recoil.useCallbackRef
                (fun (setter: CallbackMethods) ->
                    promise {
                        let eventId = Recoil.Atoms.Events.EventId (JS.Constructors.Date.now (), Guid.NewGuid ())

                        let! name = setter.snapshot.getReadWritePromise Recoil.Atoms.Database.name input.DatabaseId

                        let! dayStart =
                            setter.snapshot.getReadWritePromise Recoil.Atoms.Database.dayStart input.DatabaseId

                        let! availableDatabaseIds =
                            setter.snapshot.getPromise (Recoil.Atoms.Session.availableDatabaseIds input.Username)

                        let event = Recoil.Atoms.Events.Event.AddDatabase (eventId, name, dayStart)

                        setter.set (Recoil.Atoms.Events.events eventId, event)

                        let! databaseStateMapCache =
                            setter.snapshot.getPromise (Recoil.Atoms.Session.databaseStateMapCache input.Username)

                        let newDatabaseStateMapCache =
                            databaseStateMapCache
                            |> Map.add
                                input.DatabaseId
                                {
                                    Database =
                                        {
                                            Id = input.DatabaseId
                                            Name = name
                                            Owner = input.Username
                                            SharedWith =
                                                DatabaseAccess.Private [
                                                    DatabaseAccessItem.Admin input.Username
                                                ]
                                            Position = None
                                            DayStart = dayStart
                                        }
                                    InformationStateMap = Map.empty
                                    TaskStateMap = Map.empty
                                }

                        printfn
                            $"DatabaseForm():
                        databaseStateMapCache.Count={databaseStateMapCache.Count}
                        newDatabaseStateMapCache.Count={newDatabaseStateMapCache.Count}"

                        setter.set (Recoil.Atoms.Session.databaseStateMapCache input.Username, newDatabaseStateMapCache)

                        setter.set (
                            Recoil.Atoms.Session.availableDatabaseIds input.Username,
                            (input.DatabaseId :: availableDatabaseIds)
                        )


                        printfn $"event {event}"
                        do! input.OnSave ()
                    })

        Chakra.stack
            {| spacing = "25px" |}
            [
                Chakra.box
                    {| fontSize = "15px" |}
                    [
                        str "Add Database"
                    ]

                Chakra.stack
                    {| spacing = "15px" |}
                    [
                        Input.Input (
                            jsOptions<_>
                                (fun x ->
                                    x.autoFocus <- true
                                    x.label <- "Name"
                                    x.placeholder <- sprintf "new-database-%s" (DateTime.Now.Format "yyyy-MM-dd")
                                    x.atom <- Some (Recoil.AtomFamily (Recoil.Atoms.Database.name, input.DatabaseId))
                                    x.onFormat <- Some (fun (DatabaseName name) -> name)
                                    x.onValidate <- Some (DatabaseName >> Some)
                                    x.onEnterPress <- Some onSave)
                        )

                        Input.Input (
                            jsOptions<_>
                                (fun x ->
                                    x.label <- "Day starts at"
                                    x.placeholder <- "00:00"

                                    x.atom <-
                                        Some (Recoil.AtomFamily (Recoil.Atoms.Database.dayStart, input.DatabaseId))

                                    x.inputFormat <- Some Input.InputFormat.Time
                                    x.onFormat <- Some (fun time -> time.Stringify ())
                                    x.onValidate <- Some (DateTime.Parse >> FlukeTime.FromDateTime >> Some))
                        )

                        Chakra.stack
                            {|
                                direction = "row"
                                align = "center"
                            |}
                            [
                                Chakra.box
                                    {|  |}
                                    [
                                        str "Access:"
                                    ]

                                DatabaseAccessIndicator.DatabaseAccessIndicator ()
                            ]
                    ]

                Chakra.button
                    {| onClick = onSave |}
                    [
                        str "Save"
                    ]
            ]
