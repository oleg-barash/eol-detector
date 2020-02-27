open System
open System.IO
open System.Linq
open System.Text.RegularExpressions

type EOLInfo = {
    HasCRLF: bool
    HasCR: bool
    HasLF: bool
}
type FileInfo = {
    FilePath: string
    EOLInfo: EOLInfo
}
let problemFiles = []
let windowsEOLRegex = "\r\n"
let linuxEOLRegex = "[^\r]\n"
let macEOLRegex = "\r[^\n]"
let getFileEOLInfo (fileStream: FileStream): EOLInfo = 
    let reader: StreamReader = new StreamReader(fileStream)
    let res = reader.ReadToEnd()
    {
      HasCRLF=Regex.IsMatch(res, windowsEOLRegex)
      HasCR=Regex.IsMatch(res, linuxEOLRegex)
      HasLF=Regex.IsMatch(res, macEOLRegex)
    }

let rec findMixedEOL folderPath filePattern : seq<FileInfo> =
    let dirs = Directory.EnumerateDirectories(folderPath)
    let res = if dirs.Any() then
                dirs
                |> Seq.map(fun dir -> findMixedEOL dir filePattern)
                |> Seq.reduce(fun files_1 files -> Seq.append files_1 files)
              else Seq.empty
    let files = Directory.EnumerateFiles(folderPath)
                |> Seq.filter(fun f -> Regex.IsMatch(f, filePattern))
                |> Seq.map(fun f -> { FilePath=Path.Combine(folderPath, f); EOLInfo= getFileEOLInfo (Path.Combine (folderPath, f) |> File.OpenRead )})
                |> Seq.filter(fun file -> file.EOLInfo.HasCRLF && file.EOLInfo.HasCR || file.EOLInfo.HasCRLF && file.EOLInfo.HasLF || file.EOLInfo.HasLF && file.EOLInfo.HasCR)
    Seq.append res files
    
let rec findLinuxFiles folderPath filePattern : seq<FileInfo> =
    let dirs = Directory.EnumerateDirectories(folderPath)
    let res = if dirs.Any() then
                dirs
                |> Seq.map(fun dir -> findLinuxFiles dir filePattern)
                |> Seq.reduce(fun files_1 files -> Seq.append files_1 files)
              else Seq.empty
    let files = Directory.EnumerateFiles(folderPath)
                |> Seq.filter(fun f -> Regex.IsMatch(f, filePattern))
                |> Seq.map(fun f -> { FilePath=Path.Combine(folderPath, f); EOLInfo= getFileEOLInfo (Path.Combine (folderPath, f) |> File.OpenRead )})
                |> Seq.filter(fun file -> file.EOLInfo.HasLF)
    Seq.append res files

[<EntryPoint>]
let main argv =
    findLinuxFiles argv.[0] argv.[1]
    |> Seq.iter(fun file -> Console.WriteLine(file.FilePath))
    0
