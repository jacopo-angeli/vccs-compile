open System.IO
open FSharp.Text.Lexing
open Compiler

[<EntryPoint>]
let main argv =

    let rec dumpTokens lexbuf =
        match Lexer.token lexbuf with
        | Parser.EOF -> printfn "EOF"
        | token ->
            printfn "Token: %A" token
            dumpTokens lexbuf

    let input = "Q (x:[0,1]) = Q(x);"
    let lexbuf = LexBuffer<char>.FromString input
    dumpTokens lexbuf


    let testFile = "test/full.ccsvp"
    let input = File.ReadAllText testFile
    let lexbuf = LexBuffer<char>.FromString input

    try
        let Vccss = Parser.start Lexer.token lexbuf
        printfn "✅ Parsed successfully.\n\n"
        List.iter (fun x -> printfn "%s \t (%O)" (Vccs.stringify x) x) Vccss
        let Pccss = compile Vccss
        printfn "✅ Compiled successfully.\n\n"
        List.iter (fun x -> printfn "%s" (Pccs.stringify x)) Pccss
        0
    with ex ->
        let pos = lexbuf.EndPos
        let line = if pos.Line > 0 then pos.Line else 1
        let column = if pos.Column > 0 then pos.Column else 1
        printfn "%O" ex
        0
