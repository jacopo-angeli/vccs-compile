module RandomVccsTester

open System
open FSharp.Text.Lexing
open Compiler


let rand = Random()

let pick lst = List.item (rand.Next (List.length lst)) lst

let genIdent () =
    let letters = "abcdefghijklmnpqrstuvwxyz"
    let len = rand.Next(1, 4)
    String(Array.init len (fun _ -> pick (List.ofSeq letters)))

let genInterval () =
    let a = rand.Next(0, 5)
    let b = a + rand.Next(0, 3)
    sprintf "[%d,%d]" a b

let genParam () =
    let id = genIdent()
    let interval = genInterval()
    sprintf "%s : %s" id interval

let genParams () =
    let n = rand.Next(0, 3)
    List.init n (fun _ -> genParam())
    |> String.concat ", "

let genVar () = pick [ "x"; "y"; "z" ]

let genExpr () =
    let x = rand.Next(0, 10)
    let y = rand.Next(0, 10)
    pick [
        sprintf "%d" x
        genVar()
        sprintf "(%d + %d)" x y
        sprintf "(%d * %d)" x y
        sprintf "(%s + %d)" (genVar()) y
    ]

let genProcName () = pick [ "P"; "Q"; "A"; "B" ]

let genSimpleProcess () =
    let ch = pick ["a"; "b"; "c"]
    let expr = genExpr()
    sprintf "'%s(%s).0" ch expr

let genFullDefinition () =
    let name = genProcName()
    let parameters = genParams()
    let body = genSimpleProcess()
    if parameters = "" then
        sprintf "%s = %s;" name body
    else
        sprintf "%s(%s) = %s;" name parameters body

let genProgram (n: int) =
    List.init n (fun _ -> genFullDefinition())
    |> String.concat "\n"

/// Parses a randomly generated Vccs program
let testParseRandomProgram () =
    let input = genProgram 2
    printfn "Generated program:\n%s" input
    let lexbuf = LexBuffer<char>.FromString input
    let vccss = 
        try
            let Vccss = Parser.start Lexer.token lexbuf
            printfn "\n✅ Parsed successfully.\n"
            List.iter (fun x -> printfn "%s \t (%O)" (Vccs.stringify x) x) Vccss
            Vccss
        with e ->
            failwith"\n❌ Parsing fail.\n"

    let pccss =
        try
            let pccss = compile vccss
            printfn "\n✅ Compiled successfully.\n"
            List.iter (fun x -> printfn "%s" (Pccs.stringify x)) pccss
            pccss
        with _ ->
            failwith "\n❌ Compilation fail.\n"
    0
