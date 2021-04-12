namespace Fluke.UI.Frontend

open System
open System.Collections.Generic
open Feliz.Recoil
open Fluke.UI.Frontend


module Profiling =
    let private initialTicks = DateTime.Now.Ticks

    let private ticksDiff ticks =
        int64 (TimeSpan(ticks - initialTicks).TotalMilliseconds)

    let internal profilingState =
        {|
            CallCount = Dictionary<string, int> ()
            Timestamps = List<string * int64> ()
        |}

    Bindings.Dom.set (nameof profilingState) profilingState

    let internal addCount id =
        match profilingState.CallCount.ContainsKey id with
        | false -> profilingState.CallCount.[id] <- 1
        | true -> profilingState.CallCount.[id] <- profilingState.CallCount.[id] + 1

    let internal addTimestamp id =
        profilingState.Timestamps.Add (id, ticksDiff DateTime.Now.Ticks)

    addTimestamp "Init"