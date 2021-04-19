namespace Fluke.UI.Frontend.Components

open Fable.React
open Fable.Core.JsInterop
open Feliz
open Feliz.UseListener
open Feliz.Recoil
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Bindings
open Fable.Core
open Fluke.UI.Frontend.Hooks


module DebugOverlay =

    [<ReactComponent>]
    let DebugOverlay () =
        let text, setText = React.useState ""
        let oldJson, setOldJson = React.useState ""
        let debug, setDebug = Recoil.useState Recoil.Atoms.debug

        Scheduling.useScheduling
            Scheduling.Interval
            1000
            (fun () ->
                if not debug then
                    ()
                else
                    let json =
                        {|
                            CallCount =
                                Profiling.profilingState.CallCount
                                |> Seq.map (fun (KeyValue (k, v)) -> k, box <| string v)
                                |> JsInterop.createObj
                            Timestamps =
                                Profiling.profilingState.Timestamps
                                |> Seq.map (fun (k, v) -> $"{k} = {v}")
                                |> Seq.toArray
                        |}
                        |> fun obj -> JS.JSON.stringify (obj, unbox null, 4)

                    if json = oldJson then
                        ()
                    else
                        setText json
                        setOldJson json)

        React.fragment [
            if debug then
                Chakra.box
                    (fun x ->
                        x.id <- "test1"
                        x.position <- "absolute"
                        x.width <- "100px"
                        x.height <- "80px"
                        x.top <- "40px"
                        x.right <- "0"
                        x.backgroundColor <- "#ccc3"
                        x.zIndex <- 1)
                    [
                        str "test1"
                    ]

            Chakra.box
                (fun x ->
                    x.width <- "min-content"
                    x.height <- if debug then "80%" else "initial"
                    x.position <- "fixed"
                    x.right <- "0"
                    x.bottom <- "0"
                    x.fontSize <- "9px"
                    x.backgroundColor <- "#44444488"
                    x.zIndex <- 1
                    x.overflow <- if debug then "scroll" else "initial")
                [
                    Checkbox.Checkbox
                        (fun x ->
                            x.isChecked <- debug
                            x.onChange <- fun _ -> promise { setDebug (not debug) })
                        [
                            str (if debug then "Debug" else "")
                        ]

                    if debug then
                        Html.pre [
                            prop.id "diag"
                            prop.children [ str text ]
                        ]
                ]
        ]
