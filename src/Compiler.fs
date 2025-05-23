module Compiler

open Vccs
open Pccs
open Interval

let evaluateA (expression: Vccs.AExp) =
    match expression with
    | Num x -> x
    | Var x -> 999
    | _ -> failwith "Not Implemented"

let evaluateB (expression: Vccs.BExp) : bool = failwith "Not Implemented"
let ty2val ((lo, up): Interval) = [ lo..up ]

let compile (ccss: Vccs list) : Pccs list =
    // ! se trovo un input devo aver dichiarato la variabile che utilizzo nella dichiarazione iniziale ?
    // ! No check on redundancy

    let rec R (P: Vccs.P) =
        match P with
        | Vccs.ConstCall(K, []) ->
            let _, P =
                try
                    match List.find (fun (id, _, _) -> id = K) ccss with
                    | _, args, body -> args, body
                with _ ->
                    failwith "Process %s called before decalaration."

            Pccs.ConstCall(K)

        | Vccs.ConstCall(K, expressions) ->
            let args, P =
                match List.find (fun (id, _, _) -> id = K) ccss with
                | _, args, body -> args, body

            let renames =
                List.map2
                    (fun x y -> RenameVal(x, y))
                    (List.map evaluateA expressions)
                    (List.map (fun (v, _) -> v) args)

            R (Vccs.Rename(P, renames))

        | Vccs.Act(alpha, P)->
            match alpha with
            | Vccs.Input(channel, parameters, P) ->
                let aux =
                    List.map
                        (fun (var, ty) -> List.map (fun value -> Pccs.Input(sprintf "%s_%d" var value, (R P))) (ty2val ty))
                        parameters

                List.reduce
                    (fun x y -> Pccs.Sum(x, y))
                    (List.map (fun sublist -> List.reduce (fun x y -> Pccs.Sum(x, y)) sublist) aux)
            | Vccs.Output(channel, expression, next) -> Pccs.Output(sprintf "%s_%s" channel (Interval.stringify (evaluateA expression)), R next)
            | Vccs.Silent(next) -> Pccs.Silent(R next)

        | Vccs.Conditional(expression, P) ->
            match evaluateB expression with
            | true -> R P
            | false -> Pccs.Nil

        | Vccs.Sum(P, Q) -> Pccs.Sum(R P, R Q)

        | Vccs.Parallel(P, Q) -> Pccs.Parallel(R P, R Q)

        | Vccs.Restrict(P, channels) -> failwith "Not Implemented"

        | Vccs.Rename(P, renames) -> 
            // f'(an) = f(a)n
            // for every possible value i need to rename the
            // for every action i need to expand the list with the correct expansions

            let act2act (action : Vccs.Action) : Pccs.Action list =
                match action with
                | Vccs.Silent -> [Pccs.Silent]
                | Vccs.Input(channel, (_, ty)) ->
                    let values = ty2val ty
                    List.map (fun value -> Pccs.Input $"%s{channel}_%d{value}") values
                | Vccs.Output(channel, expression) ->
                    let value = evaluateA expression
                    [Pccs.Output(sprintf "%s_%d" channel value)] 

            let f': (Pccs.Action * Pccs.Action) list  = 
                List.map 
                    (
                        fun (x,y) ->
                            let leftside = act2act x
                            List.map (fun action -> (action, y)) leftside
                    ) 
                    renames

            let f'' : (Pccs.Action * Pccs.Action) list = List.reduce List.append f'

            Pccs.Rename(R P, f'')
        | Vccs.Nil -> Pccs.Nil

    List.map (fun (K, _, P) -> Pccs(K, R P)) ccss
