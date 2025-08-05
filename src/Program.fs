open System.IO
open System
open FSharp.Text.Lexing
open Compiler

let getOutputFileName (inputFile: string) =
    let dir = Path.GetDirectoryName(inputFile)
    let fileName = Path.GetFileNameWithoutExtension(inputFile)
    let ext = Path.GetExtension(inputFile)
    let outputFileName = $"{fileName}_ccs{ext}"
    if String.IsNullOrEmpty(dir) then outputFileName
    else Path.Combine(dir, outputFileName)

[<EntryPoint>]
let main argv =
    if argv.Length = 0 then
        printfn "Usage: V2Pccs <inputfile>"
        1
    else
        let inputFile = argv.[0]
        if not (File.Exists inputFile) then
            printfn "Input file '%s' does not exist." inputFile
            1
        else
            let input = File.ReadAllText inputFile
            let lexbuf = LexBuffer<char>.FromString input

            let vccss =
                try
                    let Vccss = Parser.start Lexer.token lexbuf
                    printfn "\n✅ Parsed successfully.\n"
                    Vccss
                with e ->
                    printf "\n❌ Parsing fail:\n%O\n\n." e.Message
                    []

            let pccss =
                try
                    let pccss = compile vccss
                    printfn "\n✅ Compiled successfully.\n"
                    pccss
                with e ->
                    printf "\n❌ Compilation fail:\n%O\n\n." e.Message
                    []

            let outputFile = getOutputFileName inputFile
            let outputContent = String.concat "\n" (List.map Pccs.stringify pccss)
            File.WriteAllText(outputFile, outputContent)
            printfn "Output written to: %s" outputFile
            0
