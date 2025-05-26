open System.IO
open FSharp.Text.Lexing
open Compiler

[<EntryPoint>]
let main argv =

    // let rec dumpTokens lexbuf =
    //     match Lexer.token lexbuf with
    //     | Parser.EOF -> printfn "EOF"
    //     | token ->
    //         printfn "Token: %A" token
    //         dumpTokens lexbuf

    // let input = "Q (x:[0,1]) = Q(x);"
    // let lexbuf = LexBuffer<char>.FromString input
    // dumpTokens lexbuf


    let testFile = "test/full.ccsvp"
    let input = File.ReadAllText testFile
    let lexbuf = LexBuffer<char>.FromString input
    
    let vccss = 
        try
            let Vccss = Parser.start Lexer.token lexbuf
            printfn "\n✅ Parsed successfully.\n"
            List.iter (fun x -> printfn "%s \t (%O)" (Vccs.stringify x) x) Vccss
            Vccss
        with e ->
            printf "\n❌ Parsing fail:\n%O\n\n." e.Message
            []

    let pccss =
        try
            let pccss = compile vccss
            printfn "\n✅ Compiled successfully.\n"
            List.iter (fun x -> printfn "%s" (Pccs.stringify x)) pccss
            pccss
        with e ->
            printf "\n❌ Compilation fail:\n%O\n\n." e.Message
            []
    0
