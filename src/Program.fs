open System.IO
open System
open FSharp.Text.Lexing
open Compiler

let getOutputFileName (inputFile: string) =
    let dir = Path.GetDirectoryName(inputFile)
    let fileName = Path.GetFileNameWithoutExtension(inputFile)
    let outputFileName = $"{fileName}.ccs"
    if String.IsNullOrEmpty(dir) then outputFileName
    else Path.Combine(dir, outputFileName)

let rec collectRefs p =
    match p with
    | Pccs.ConstCall name -> [name]
    | Pccs.Act(_, p) -> collectRefs p
    | Pccs.Sum(p1, p2) | Pccs.Parallel(p1, p2) -> collectRefs p1 @ collectRefs p2
    | Pccs.Restrict(p, _) | Pccs.Rename(p, _) -> collectRefs p
    | Pccs.Nil -> []

let parseInput input =
    let lexbuf = LexBuffer<char>.FromString input
    Parser.start Lexer.token lexbuf

let repl (stringify: Pccs.Pccs -> string) =
    printfn "VP2Pccs interactive - CCS-VP to CCS compiler"
    printfn "Enter declarations ending with ';'. Multi-line input is supported."
    printfn "Commands: #list, #clear, #quit"
    printfn ""

    let mutable defined: Set<string> = Set.empty
    let mutable running = true

    while running do
        printf "vccs> "
        Console.Out.Flush()

        let mutable buffer = ""
        let mutable complete = false

        while not complete do
            let line = Console.ReadLine()
            if line = null then
                complete <- true
                running <- false
            else
                buffer <- buffer + line + "\n"
                let trimmed = buffer.TrimEnd()
                if trimmed.StartsWith "#" || trimmed.EndsWith ";" then
                    complete <- true
                else
                    printf "    | "
                    Console.Out.Flush()

        let input = buffer.Trim()

        match input with
        | "#quit" | "#exit" ->
            running <- false
        | "#clear" ->
            defined <- Set.empty
            printfn "Context cleared."
        | "#list" ->
            if Set.isEmpty defined then
                printfn "(no definitions)"
            else
                defined |> Set.iter (fun name -> printfn "  %s" name)
        | "" -> ()
        | _ ->
            try
                let vccss = parseInput input
                let pccss =
                    try compile vccss
                    with e ->
                        eprintfn "❌ Compilation error: %s" e.Message
                        []
                let batchDefined = pccss |> List.map fst |> Set.ofList
                let allDefined = Set.union defined batchDefined
                pccss |> List.iter (fun (_, body) ->
                    collectRefs body |> List.iter (fun name ->
                        if not (Set.contains name allDefined) then
                            eprintfn "⚠️  Warning: undefined reference '%s'" name))
                pccss |> List.iter (fun pccs -> printfn "%s" (stringify pccs))
                defined <- allDefined
            with e ->
                eprintfn "❌ Parse error: %s" e.Message

[<EntryPoint>]
let main argv =
    let flags  = argv |> Array.filter (fun a -> a.StartsWith "--")
    let args   = argv |> Array.filter (fun a -> not (a.StartsWith "--"))
    let caalMode = Array.contains "--caal" flags
    let stringify = if caalMode then Pccs.stringifyCaal else Pccs.stringify

    if args.Length = 0 then
        repl stringify
        0
    else
        let inputFile = args.[0]
        if not (File.Exists inputFile) then
            printfn "Input file '%s' does not exist." inputFile
            1
        else
            let input = File.ReadAllText inputFile
            let lexbuf = LexBuffer<char>.FromString input

            let vccss =
                try
                    let vccss = Parser.start Lexer.token lexbuf
                    printfn "\n✅ Parsed successfully.\n"
                    vccss
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

            let defined = pccss |> List.map fst |> Set.ofList
            pccss |> List.iter (fun (_, body) ->
                collectRefs body |> List.iter (fun name ->
                    if not (Set.contains name defined) then
                        printfn "⚠️  Warning: undefined reference '%s'" name))

            let outputFile = getOutputFileName inputFile
            let outputContent = String.concat "\n" (List.map stringify pccss)
            File.WriteAllText(outputFile, outputContent)
            printfn "Output written to: %s" outputFile
            0
