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

let rec stringify ((K, P) : Pccs) : string =
    let actToStr = function
        | Input ch -> ch
        | Output ch -> "'" + ch
        | Silent -> "τ"

    let renameToStr (aFrom, aTo) =
        sprintf "%s → %s" (actToStr aFrom) (actToStr aTo)

    let rec P2S proc =
        match proc with
        | ConstCall name -> name
        | Act (action, nextProc) -> sprintf "%s.%s" (actToStr action) (P2S nextProc)
        | Sum (proc1, proc2) -> sprintf "(%s + %s)" (P2S proc1) (P2S proc2)
        | Parallel (proc1, proc2) -> sprintf "(%s | %s)" (P2S proc1) (P2S proc2)
        | Restrict (proc, names) -> sprintf "%s \\ {%s}" (P2S proc) (String.concat ", " names)
        | Rename (proc, renames) ->
            let renStr = renames |> List.map renameToStr |> String.concat ", "
            sprintf "%s [%s]" (P2S proc) renStr
        | Nil -> "0"

    sprintf "%s = %s" K (P2S P)
