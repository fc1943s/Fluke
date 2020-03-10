namespace MechaHaze.UI.Frontend

open Fable.React
open Fable.React.Props
open Fluke.Shared
open Fluke.UI.Frontend
    
module CellComponent =

    type Props =
        { Date: Model.FlukeDate
          Task: Model.Task
          Status: Model.CellStatus
          Today: Model.FlukeDate }

    type State =
        { a: unit }
        static member inline Default =
            { a = () }
        
    type Message =
        | A of unit
        
        
    let ``default`` = FunctionComponent.Of (fun props ->
        div [ Class "cell"
              Style [ Position PositionOptions.Relative ] ][
            
            let cellComments =
                PrivateData.cellComments
                |> List.filter (fun cell -> cell.Task.Name = props.Task.Name && cell.Date = props.Date)
            let hasComments = cellComments |> List.isEmpty |> not
                
            div [ Style [ Width 18
                          Height 18
                          if hasComments then
                              Border "1px solid #ffffff77"
                          Opacity (if props.Date = props.Today then 0.8 else 1.)
                          BackgroundColor props.Status.CellColor ] ][]
                
            if hasComments then
                div [ Style [ Position PositionOptions.Absolute
                              BorderTop "8px solid #f00"
                              BorderLeft "8px solid transparent"
                              Right 0
                              Top 0 ] ][]
                
                div [ Class "comment-container"
                      Style [ Position PositionOptions.Absolute
                              Padding 20
                              MinWidth 200
                              BackgroundColor "#000"
                              Opacity 0.4
                              Left 18
                              ZIndex 1
                              Top 0 ] ][
                    
                    cellComments
                    |> List.map (fun comment ->
                        div [ Key (props.Date.ToString ()) ][
                            ReactBindings.React.createElement
                                (Ext.reactMarkdown,
                                    {| source = comment.Comment |}, [])
                        ]
                    ) |> ofList
                ]
        ]
    , memoizeWith = equalsButFunctions)
