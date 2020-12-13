namespace Fluke.UI.Frontend.Components

open FSharpPlus
open Feliz
open Feliz.Recoil
open Fluke.UI.Frontend
open Fluke.UI.Frontend.Bindings
open Fluke.Shared


module HabitTrackerView =
    open Domain.UserInteraction

    [<ReactComponent>]
    let HabitTrackerView (input: {| Username: Username |}) =
        let taskIdList = Recoil.useValue (Recoil.Atoms.Session.taskIdList input.Username)

        Chakra.flex
            {|  |}
            [
                Chakra.box
                    {|  |}
                    [
                        yield!
                            Chakra.box {| height = "17px" |} []
                            |> List.replicate 3

                        Chakra.flex
                            {|  |}
                            [
                                Chakra.box
                                    {| paddingRight = "10px" |}
                                    [
                                        yield!
                                            taskIdList
                                            |> List.map (fun taskId ->
                                                TaskInformationName.TaskInformationName {| TaskId = taskId |})
                                    ]
                                // Column: Priority
                                Chakra.box
                                    {| paddingRight = "10px"; textAlign = "center" |}
                                    [
                                        yield!
                                            taskIdList
                                            |> List.map (fun taskId -> TaskPriority.TaskPriority {| TaskId = taskId |})
                                    ]
                                // Column: Task Name
                                Chakra.box
                                    {| width = "200px" |}
                                    [
                                        yield!
                                            taskIdList
                                            |> List.map (fun taskId -> TaskName.TaskName {| TaskId = taskId |})
                                    ]
                            ]
                    ]
                Chakra.box
                    {|  |}
                    [
                        GridHeader.GridHeader {| Username = input.Username |}
                        Cells.Cells
                            {|
                                Username = input.Username
                                TaskIdList = taskIdList
                            |}
                    ]
            ]
