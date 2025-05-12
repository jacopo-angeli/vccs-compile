module VccsPrinter

open Vccs

let rec printAExp exp =
    match exp with
    | Num n              -> string n
    | Var v              -> v
    | Add (l, r)         -> "(" + printAExp l + " + " + printAExp r + ")"
    | Sub (l, r)         -> "(" + printAExp l + " - " + printAExp r + ")"
    | Mul (l, r)         -> "(" + printAExp l + " * " + printAExp r + ")"
    | Div (l, r)         -> "(" + printAExp l + " / " + printAExp r + ")"

let rec printBExp bexp =
    match bexp with
    | Eq (l, r)          -> "(" + printAExp l + " == " + printAExp r + ")"
    | Neq (l, r)         -> "(" + printAExp l + " != " + printAExp r + ")"
    | Gt (l, r)          -> "(" + printAExp l + " > "  + printAExp r + ")"
    | Ge (l, r)          -> "(" + printAExp l + " >= " + printAExp r + ")"
    | Le (l, r)          -> "(" + printAExp l + " <= " + printAExp r + ")"
    | Not b              -> "!(" + printBExp b + ")"
    | And (l, r)         -> "(" + printBExp l + " && " + printBExp r + ")"
    | Or (l, r)          -> "(" + printBExp l + " || " + printBExp r + ")"

let rec printVccs proc =
    match proc with
    | ConstCall (name, args) ->
        name + "(" + String.concat ", " (List.map printAExp args) + ")"
    | Input (ch, x, p) ->
        ch + "(" + x + ") . " + printVccs p
    | Output (ch, v, p) ->
        ch + "<" + printAExp v + "> . " + printVccs p
    | Silent p ->
        "τ . " + printVccs p
    | Conditional (b, p) ->
        "if " + printBExp b + " then " + printVccs p
    | Sum (p1, p2) ->
        "(" + printVccs p1 + " + " + printVccs p2 + ")"
    | Parallel (p1, p2) ->
        "(" + printVccs p1 + " | " + printVccs p2 + ")"
    | Restrict (p, names) ->
        printVccs p + " \\ {" + String.concat ", " names + "}"
    | Rename (p, renames) ->
        let renStr = renames |> List.map (fun (a, b) -> a + "→" + b) |> String.concat ", "
        printVccs p + " [" + renStr + "]"