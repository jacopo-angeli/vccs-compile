open System.IO
open FSharp.Text.Lexing
open Compiler

let parse input =
    let lexbuf = LexBuffer<char>.FromString(input)
    try
        let result = Parser.start Lexer.token lexbuf
        printfn "✅ Parsed successfully."
        result
    with ex ->
        printfn "❌ Parsing error: %s" ex.Message
        reraise ()

[<EntryPoint>]
let main argv =
    
    let rec dumpTokens lexbuf =
        match Lexer.token lexbuf with
        | Parser.EOF -> printfn "EOF"
        | token -> 
            printfn "Token: %A" token
            dumpTokens lexbuf
    let input = "P = a(0).A;"
    let lexbuf = LexBuffer<char>.FromString input
    dumpTokens lexbuf


    let testFile = "test/full.ccsvp"
    let input = File.ReadAllText testFile
    let lexbuf = LexBuffer<char>.FromString input
    try
        let result = Parser.start Lexer.token lexbuf 
        printfn "✅ Parsed successfully.\n"
        List.iter (fun x -> printfn "%O\n"  x) result
        let ignore = (compile result)
        0
    with
    | ex ->
        let pos = lexbuf.EndPos
        let line = if pos.Line > 0 then pos.Line else 1
        let column = if pos.Column > 0 then pos.Column else 1
        printfn "%O" ex
        0