module Compiler
open Vccs
open Pccs

let evaluateA (expression : Vccs.AExp) : int = 
    match expression with
    | Num x -> x
    | _ -> failwith "Not Implemented"

let evaluateB (expression : Vccs.BExp) : bool = failwith "Not Implemented"
let ty2val (ty : Vccs.ty) : int list = failwith "Not Implemented"

let compile (ccss: Vccs list) : Pccs list =
    // ! se trovo un input devo aver dichiarato la variabile che utilizzo nella dichiarazione iniziale ?
    // ! No check on redundancy
    let onlyvars = List.concat (List.map (fun (_, parameters, _) -> parameters) ccss)
    printfn "%O" onlyvars
    let rec R (P : Vccs.P)= 
        match P with
        | Vccs.ConstCall(K,[]) -> 
            let _, P  = 
                try
                    match List.find (fun (id, _, _) -> id = K) ccss with _, args, body -> args, body
                with | _ ->
                    failwith "Process %s called before decalaration."
            Pccs.ConstCall(K)

        | Vccs.ConstCall(K, expressions) -> 
            let args, P  = match List.find (fun (id, _, _) -> id = K) ccss with _, args, body -> args, body
            let renames = List.map2 (fun x y -> RenameVal(x,y)) (List.map evaluateA expressions) (List.map (fun (v, _) -> v) args)
            R (Vccs.Rename(P, renames))

        | Vccs.Input(channel, variable, P) ->
            // Serve il tipo di variabile
            printfn "Input action"
            let _, ty = List.find (fun (id, _) -> id = variable ) onlyvars
            let ccsinputs = List.map (fun v -> Pccs.Input(sprintf "%s_%d" channel v, R (Vccs.Rename(P, [RenameVal(v, variable)])))) (ty2val ty)
            List.reduce (fun x y -> Pccs.Sum(x,y))  ccsinputs
        
        | Vccs.Output(channel, expression, next) -> 
            Pccs.Output(sprintf "%s_%d" channel (evaluateA expression), R next)
        
        | Vccs.Silent(next) -> 
            Pccs.Silent(R next)
        
        | Vccs.Conditional(expression, P) -> 
            match evaluateB expression with
            | true -> R P
            | false -> Pccs.Nil
        
        | Vccs.Sum(P, Q) -> 
            Pccs.Sum(R P, R Q)
        
        | Vccs.Parallel(P, Q) -> 
            Pccs.Parallel(R P, R Q)

        | Vccs.Restrict(P, channels) -> 
            failwith "Not Implemented" 
        
        | Vccs.Rename(_, _) -> 
            failwith "Not Implemented"

        | Vccs.Nil ->
            Pccs.Nil
    
    printfn "%O" ccss
    List.map (fun (K, _, P) -> Pccs(K, R P)) ccss