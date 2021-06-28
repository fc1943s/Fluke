namespace Fluke.UI.Frontend.Hooks

open Fluke.UI.Frontend.State

#nowarn "40"

open Feliz
open Fluke.UI.Frontend.Bindings
open Fable.Core


module Theme =
    let private theme
        (input: {| DarkMode: bool
                   SystemUiFont: bool
                   FontSize: int |})
        =
        let alphaColors dark =
            let n = if dark then "0" else "255"

            {|
                ``50`` = $"rgba({n}, {n}, {n}, 0.04)"
                ``100`` = $"rgba({n}, {n}, {n}, 0.06)"
                ``200`` = $"rgba({n}, {n}, {n}, 0.08)"
                ``300`` = $"rgba({n}, {n}, {n}, 0.16)"
                ``400`` = $"rgba({n}, {n}, {n}, 0.24)"
                ``500`` = $"rgba({n}, {n}, {n}, 0.36)"
                ``600`` = $"rgba({n}, {n}, {n}, 0.48)"
                ``700`` = $"rgba({n}, {n}, {n}, 0.64)"
                ``800`` = $"rgba({n}, {n}, {n}, 0.80)"
                ``900`` = $"rgba({n}, {n}, {n}, 0.92)"
            |}


        let colors =
            {|
                heliotrope = "#b586ff"
                gray =
                    {|
                        ``10`` = if input.DarkMode then "#1a1a1a" else "#e5e5e5" // grayDark
                        ``13`` = if input.DarkMode then "#212121" else "#dedede" // grayLight
                        ``16`` = if input.DarkMode then "#292929" else "#d6d6d6" // grayLighter
                        ``45`` = if input.DarkMode then "#727272" else "#8d8d8d" // textDark
                        ``77`` = if input.DarkMode then "#b0bec5" else "#4f413a" // textLight
                        ``87`` = if input.DarkMode then "#dddddd" else "#222222" // text
                    |}
                whiteAlpha = alphaColors (not input.DarkMode)
                blackAlpha = alphaColors input.DarkMode
                orange = if input.DarkMode then "#ffb836" else "#AF750B"
                green = if input.DarkMode then "#a4ff8d" else "#269309" //https://paletton.com/#uid=12K0u0kt+lZlOstrKqzzSiaJidt
            |}

        let focusShadow = $"0 0 0 1px {colors.heliotrope} !important"

        {|
            config =
                {|
                    initialColorMode = if input.DarkMode then "dark" else "light"
                    useSystemColorMode = false
                |}
            breakpoints =
                {|
                    sm = "350px"
                    md = "750px"
                    lg = "1000px"
                    xl = "1900px"
                |}
            colors = colors
            fonts =
                {|
                    main =
                        if input.SystemUiFont then
                            "system-ui"
                        else
                            "'Roboto Condensed', sans-serif"
                |}
            fontWeights =
                {|
                    hairline = 100
                    thin = 200
                    light = 300
                    normal = 400
                    medium = 500
                    semibold = 600
                    bold = 700
                    extrabold = 800
                    black = 900
                |}
            fontSizes = {| main = $"{input.FontSize}px" |}
            lineHeights = {| main = $"{input.FontSize}px" |}
            styles =
                {|
                    ``global`` =
                        fun (_props: {| _colorMode: string |}) ->
                            {|
                                ``:root`` =
                                    {|
                                        ``--chakra-shadows-outline`` = focusShadow
                                    |}
                                ``*, *::before, *::after`` = {| wordWrap = "break-word" |}
                                html =
                                    {|
                                        fontSize = "main"
                                        overflow = "hidden"
                                    |}
                                body =
                                    {|
                                        fontFamily = "main"
                                        backgroundColor = "gray.13"
                                        fontWeight = "light"
                                        letterSpacing = 0
                                        lineHeight = "main"
                                        fontFeatureSettings = "pnum"
                                        fontVariantNumeric = "proportional-nums"
                                        margin = 0
                                        padding = 0
                                        boxSizing = "border-box"
                                        fontSize = "main"
                                        color = "gray.87"
                                        userSelect = "none"
                                        touchAction = "pan-x pan-y"
                                        overflow = "hidden"
                                    |}
                                ``input::-ms-reveal`` =
                                    {|
                                        filter = if input.DarkMode then "invert(1)" else ""
                                    |}
                                ``input::-ms-clear`` =
                                    {|
                                        filter = if input.DarkMode then "invert(1)" else ""
                                    |}
                                ``*::-webkit-calendar-picker-indicator`` =
                                    {|
                                        filter = if input.DarkMode then "invert(1)" else ""
                                    |}
                                ``*::-webkit-scrollbar`` = {| width = "9px" |}
                                ``*::-webkit-scrollbar:horizontal`` = {| height = "6px" |}
                                ``*::-webkit-scrollbar-track`` = {| display = "none" |}
                                ``*::-webkit-scrollbar-corner`` = {| display = "none" |}
                                ``*::-webkit-scrollbar-thumb`` =
                                    {|
                                        background = "gray.45"
                                        opacity = 0.8
                                        backgroundClip = "content-box"
                                        borderLeft = "3px solid transparent"
                                    |}
                                ``*::-webkit-scrollbar-thumb:hover`` =
                                    {|
                                        background = "gray.77"
                                        backgroundClip = "content-box"
                                        borderLeft = "3px solid transparent"
                                    |}
                                ``#root`` = {| display = "flex" |}
                                ``.rct-collapse-btn`` =
                                    {|
                                        padding = "0"
                                        marginLeft = "5px"
                                        marginRight = "15px"
                                    |}
                                ``.rct-collapse-btn:focus`` = {| boxShadow = focusShadow |}
                                ``.rct-disabled .rct-checkbox svg`` = {| display = "none" |}
                                ``.rct-disabled .rct-node-icon`` = {| marginLeft = "-20px" |}
                                ``.rct-node label:hover, .rct-node label:active`` = {| background = "none" |}
                                ``.rct-node-parent:not(:first-of-type)`` = {| marginTop = "7px" |}
                                ``.rct-node:first-of-type`` = {| marginTop = "2px" |}
                                ``.rct-node-leaf`` = {| marginBottom = "-11px" |}
                                ``.rct-title`` = {| display = "contents" |}
                                ``.sketch-picker`` =
                                    {|
                                        backgroundColor =
                                            if input.DarkMode then "#333 !important" else "#CCC !important"
                                    |}
                                ``.sketch-picker span`` =
                                    {|
                                        color = if input.DarkMode then "#DDD !important" else "#222 !important"
                                    |}
                                ``.sketch-picker input`` =
                                    {|
                                        color = if input.DarkMode then "#333 !important" else "#CCC !important"
                                    |}
                                ``.markdown-container h1`` =
                                    {|
                                        borderBottomColor = "#777"
                                        borderBottomWidth = "1px"
                                        marginBottom = "3px"
                                    |}
                                ``.markdown-container li`` = {| listStyleType = "square" |}
                                ``.markdown-container ul, .tooltip-popup p`` = {| padding = "5px 0" |}
                            |}
                |}
        |}

    let useTheme () =
        let darkMode = Store.useValue Atoms.darkMode
        let systemUiFont = Store.useValue Atoms.systemUiFont
        let fontSize = Store.useValue Atoms.fontSize

        React.useMemo (
            (fun () ->
                Chakra.react.extendTheme (
                    JsInterop.toPlainJsObj (
                        theme
                            {|
                                DarkMode = darkMode
                                SystemUiFont = systemUiFont
                                FontSize = fontSize
                            |}
                    )
                )),
            [|
                box fontSize
                box systemUiFont
                box darkMode
            |]
        )
