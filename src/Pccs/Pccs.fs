module Pccs

type channel = string

type Action =
    | Input of string
    | Output of string
    | Silent

type P =
    | ConstCall         of string                
    | Act               of Action * P                
    | Sum               of P * P                   
    | Parallel          of P * P               
    | Restrict          of P * string list       
    | Rename            of P * (Action * Action) list  
    | Nil

type Pccs = string * P

let rec stringify ((K, P) : Pccs) : string =
    let rec P2S proc =
        match proc with
        | ConstCall name -> name
        | Input (ch, proc) -> ch + "." + P2S proc
        | Output (ch, proc) -> "'" + ch + "." + P2S proc
        | Silent proc -> "τ." + P2S proc
        | Sum (proc1, proc2) -> P2S proc1 + " + " + P2S proc2
        | Parallel (proc1, proc2) -> P2S proc1 + " | " + P2S proc2
        | Restrict (proc, names) -> P2S proc + " \\ {" + String.concat ", " names + "}"
        | Rename (p, renames) -> 
            let R2S rename =
                match rename with
                | In(x, y) -> sprintf "%s → %s" x y
                | Out(x, y) -> sprintf "%s → %s" x y

            let renStr = renames |> List.map R2S |> String.concat ", "
            P2S p + " [" + renStr + "]"
        | Nil -> "0"

    sprintf "%s = %s" K (P2S P)