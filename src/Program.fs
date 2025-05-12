open System
open System.IO
open Parser

[<EntryPoint>]
let main argv =
    if argv.Length <> 1 then
        printfn "Usage: VP2Pccs <input.ccsvp>"
        1
    else
        let inputPath = argv.[0]
        let outputPath = Path.Combine("out", Path.GetFileNameWithoutExtension(inputPath) + ".ccs")
        let inputText = File.ReadAllText(inputPath)

        try
            // TODO: call parser
            printfn "Parsed successfully: %s" inputPath
            File.WriteAllText(outputPath, "// compiled CCS output goes here")
            0
        with ex ->
            printfn "Error: %s" ex.Message
            1
