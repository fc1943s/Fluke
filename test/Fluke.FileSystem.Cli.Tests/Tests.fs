namespace Fluke.FileSystem.Cli

open System
open System.IO
open Expecto
open Expecto.Flip
open Fluke.Shared
open Fluke.Shared.PrivateData
open Suigetsu.Core


module Temp = // Just to load the modules. Comment the module to use TestData instead of PrivateData
    PrivateData.TempData.load ()
    PrivateData.Tasks.load ()
    PrivateData.CellEvents.load ()
    PrivateData.Journal.load ()
        
        
module Tests =
    open Model
    
    let tests = testList "Tests" [
        
        testList "FileSystem" [
            
            let directories = {|
                Main = @"C:\home"
                Others = [
                    @"C:\home\fs\onedrive\home", "onedrive"
                ]
            |}
            
            test "1" {
                
                let informationList =
                    [ TempData._projectList |> List.map Project
                      TempData._areaList |> List.map Area
                      TempData._resourceList |> List.map Resource ]
                
                informationList
                |> List.collect (List.map (function
                    | Project project -> Some ("projects", project.Name)
                    | Area area -> Some ("areas", area.Name)
                    | Resource resource -> Some ("resources", resource.Name)
                    | Archive _ -> None
                ))
                |> List.choose id
                |> List.groupBy fst
                |> List.map (fun (informationName, informationList) ->
                    informationName, informationList |> List.map snd
                )
                |> List.map (fun (informationName, informationList) ->
                    let getDirectoriesIo homePath =
                        Directory.GetDirectories (Path.Combine (homePath, informationName), "*.*")
                        |> Array.map (fun path ->
                            Path.GetDirectoryName path,
                            Path.GetFileName path,
                            (FileInfo path).Attributes.HasFlag FileAttributes.ReparsePoint
                        )
                        |> Array.toList
                        
                    let mainDirectories = getDirectoriesIo directories.Main
                    
                    let otherDirectories =
                        directories.Others
                        |> List.map (fun (otherPath, otherAlias) ->
                            getDirectoriesIo otherPath, otherAlias
                        )
                        
                    informationList, mainDirectories, otherDirectories
                )
                |> List.map (fun (informationList, mainDirectories, otherDirectories) ->
                    
                    otherDirectories
                    |> List.iter (fun (otherDirectories, otherAlias) ->
                        
                        otherDirectories
                        |> List.iter (fun (path, name, symlink) ->
                            
                            mainDirectories
                            |> List.filter (fun (_, mainName, mainSymlink) ->
                                not symlink
                                && mainSymlink
                                && mainName = (sprintf "%s-%s" name otherAlias)
                                |> not
                            )
                            |> List.map (fun (mainPath, mainName, mainSymlink) ->
                                printfn "Missing symlink: %s -> %s"
                                     (path + string Path.DirectorySeparatorChar + name)
                                     (mainPath + string Path.DirectorySeparatorChar + name + "-" + otherAlias)
                            ) |> ignore
                            
//                            x |> ignore
                        )
//                        x |> ignore
                    )
                    
                    informationList, mainDirectories, otherDirectories
                )
                |> fun x ->
                    x |> ignore
                    printfn "@ %A" x
                
                ()
            }
        ]
    ]

