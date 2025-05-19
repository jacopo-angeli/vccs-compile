module Pccs

type P =
    | ConstCall         of string                
    | Input             of string * P          
    | Output            of string * P          
    | Silent            of P                   
    | Sum               of P * P                   
    | Parallel          of P * P               
    | Restrict          of P * string list       
    | Rename            of P * (string * string) list  
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
        | Rename (p, renames) -> P2S p + "[" + List.fold (fun acc s -> if acc = "" then s else acc + ", " + s) "" (List.map (fun (x,y) -> x + "/ " + y) renames) + "]"
        | Nil -> "0"

    sprintf "%s = %s" K (P2S P)