namespace Fluke.UI.Frontend

open Browser
open Browser.Types
open Fable.Core.JsInterop

module Ext =
    importAll "typeface-roboto-condensed"

    importAll "./node_modules/@fortawesome/fontawesome-free/css/all.css"

    importAll "./node_modules/bulma/bulma.sass"
    importAll "./node_modules/bulma-extensions/dist/css/bulma-extensions.min.css"
    importAll "./node_modules/bulmaswatch/cyborg/bulmaswatch.scss"

    importAll "./public/index.scss"
    importAll "./public/index.ts"
    importAll "./public/index.tsx"
    importAll "./public/index.js"
    importAll "./public/index.jsx"


    let reactMarkdown : obj -> obj = importDefault "react-markdown"
    
    let useEventListener (_event: string) (_fn: KeyboardEvent -> unit) : unit = importDefault "@use-it/event-listener"
    
    Dom.window?Ext <-
        {| reactMarkdown = reactMarkdown
           useEventListener = useEventListener |}
