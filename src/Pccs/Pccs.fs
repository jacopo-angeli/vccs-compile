module Pccs

type channel = string

type Action =
    | Input of string
    | Output of string
    | Silent

type P =
    | ConstCall of string
    | Act of Action * P
    | Sum of P * P
    | Parallel of P * P
    | Restrict of P * string list
    | Rename of P * (Action * Action) list
    | Nil

type Pccs = string * P

let private actToStr = function
    | Input ch  -> ch
    | Output ch -> "'" + ch
    | Silent    -> "τ"

let private actToStrCaal = function
    | Input ch  -> ch
    | Output ch -> "'" + ch
    | Silent    -> "tau"

let private buildP2S toStr =
    let rec P2S proc =
        match proc with
        | ConstCall name -> name
        | Act (action, nextProc) -> sprintf "%s.%s" (toStr action) (P2S nextProc)
        | Sum (proc1, proc2) -> sprintf "(%s + %s)" (P2S proc1) (P2S proc2)
        | Parallel (proc1, proc2) -> sprintf "(%s | %s)" (P2S proc1) (P2S proc2)
        | Restrict (proc, names) -> sprintf "%s \\ {%s}" (P2S proc) (String.concat ", " names)
        | Rename (proc, renames) ->
            let renStr =
                renames
                |> List.map (fun (aFrom, aTo) -> sprintf "%s → %s" (toStr aFrom) (toStr aTo))
                |> String.concat ", "
            sprintf "%s [%s]" (P2S proc) renStr
        | Nil -> "0"
    P2S

let stringify ((K, P) : Pccs) : string =
    sprintf "%s = %s" K (buildP2S actToStr P)

let stringifyCaal ((K, P) : Pccs) : string =
    let P2S = buildP2S actToStrCaal
    let body =
        match P with
        | Rename (inner, renames) ->
            let renStr =
                renames
                |> List.choose (fun (aFrom, aTo) ->
                    match aFrom, aTo with
                    | Input f, Input t -> Some (sprintf "%s/%s" t f)
                    | _ -> None)
                |> String.concat ", "
            sprintf "%s [%s]" (P2S inner) renStr
        | _ -> P2S P
    sprintf "%s = %s;" K body
