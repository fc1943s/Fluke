namespace Fluke.UI.Frontend.Bindings

open System.Collections.Generic
open Fable.Core.JsInterop
open Fable.Core


module Dom =
    let private domRefs = Dictionary<string, obj> ()
    Browser.Dom.window?fluke <- domRefs
    let set key value = domRefs.[key] <- value

    [<Emit "new Event($0, $1)">]
    let createEvent _eventType _props = jsNative
