module Compiler

open Vccs
open Pccs
open Interval

let getValuations (parameters: (string * Interval) list) : (string * int) list list =
    
    let domains: (string * int list) list =
        parameters
        |> List.map (fun (name, (lo, hi)) -> name, [lo .. hi])

    
    let rec cartesianProduct (domains: (string * int list) list) : (string * int) list list =
        match domains with
        | [] -> [ [] ]  
        | (name, values) :: rest ->
            let restCombinations = cartesianProduct rest
            [ for value in values do
                for combo in restCombinations ->
                    (name, value) :: combo
            ]
    cartesianProduct domains

let compile (vccss: Vccs list) : Pccs list =
    
    let rec compile (vccs: Vccs) : Pccs list =
        let rec compileP  (P : Vccs.P) (env: List<string*int>) : Pccs.P =
            let rec evaluateA (exp: Vccs.AExp) : int =
                match exp with
                | Num x -> x
                | Var x -> 
                    match List.find (fun (name, _)-> name = x) env with
                        | (_, value) -> value
                | Add (l, r) -> evaluateA l + evaluateA r
                | Sub (l, r) -> evaluateA l - evaluateA r
                | Mul (l, r) -> evaluateA l * evaluateA r
                | Div (l, r) -> evaluateA l / evaluateA r            
            let rec evaluateB (exp: Vccs.BExp) : bool =
                match exp with
                    | Eq(A,A') -> evaluateA A = evaluateA A'
                    | Neq(A,A') -> evaluateA A <> evaluateA A'
                    | More(A,A') -> evaluateA A > evaluateA A'
                    | Less(A,A') -> evaluateA A < evaluateA A'
                    | MoreEq(A,A') -> evaluateA A >= evaluateA A'
                    | LessEq(A,A') -> evaluateA A <= evaluateA A'
                    | Not B -> not (evaluateB B)              
                    | And(B, B') -> evaluateB B && evaluateB B'
                    | Or(B, B') -> evaluateB B || evaluateB B'
            match P with 
            | Vccs.Act(action,P) -> 
                match action with
                | Vccs.Silent ->
                    Pccs.Act (Pccs.Silent, compileP P env)
                | Vccs.Input(channel, (var, interval)) ->
                    Interval.toList interval
                    |> List.map (
                            fun value -> Pccs.Act(Pccs.Input (sprintf "%s_%d" channel value), compileP P ((var, value) :: env))
                        )
                    |> List.reduce (fun x y -> Pccs.Sum(x,y))

                | Vccs.Output(channel, expr) ->
                    let eval:string = 
                        try sprintf "%d" (evaluateA expr)
                        with e -> printfn "%O" expr;match expr with Vccs.Var x -> sprintf "%s" x | _ -> failwith "Impossible" 

                    Pccs.Act(Pccs.Output (sprintf "%s_%s" channel eval), compileP P env)

            | Vccs.Conditional(bExp,P) -> 
                if evaluateB bExp then compileP P env else Pccs.Nil

            | Vccs.Sum(P, P')-> Pccs.Sum(compileP P env, compileP P' env)
            | Vccs.Parallel(P,P')-> Pccs.Parallel(compileP P env, compileP P' env)
            | Vccs.Restrict(P,L)-> Pccs.Restrict(compileP P env, L)
            | Vccs.Rename (P,f)->
                let vact2pacts action =
                    match action with
                    | Vccs.Silent -> [Pccs.Silent]
                    | Vccs.Input(ch, (_, ty)) ->
                        Interval.toList ty |> List.map (fun v -> Pccs.Input (sprintf "%s_%d" ch v))
                    | Vccs.Output(ch, expr) ->
                        [Pccs.Output (sprintf "%s_%d" ch (evaluateA expr))]

                let expandedRenames =
                    List.collect (fun (fromA, toA) ->
                        List.allPairs (vact2pacts fromA) (vact2pacts toA))
                            f

                Pccs.Rename(compileP P env, expandedRenames)

            | Vccs.ConstCall (K, []) ->
                Pccs.ConstCall K

            | Vccs.ConstCall (K, expressions) ->
                let evals = List.map evaluateA expressions
                let name = K + (evals |> List.map string |> List.map (sprintf "_%s") |> String.concat "")
                Pccs.ConstCall name

            | Vccs.Nil -> Pccs.Nil       
        match vccs with 
        | name, [], body ->
            let bodyCompiled = compileP body []
            [Pccs(name, bodyCompiled)]
        | name, parameters, body ->
            List.map
                (fun (env : List<string * int>) ->
                    let name = name + List.reduce (+) (List.map (fun (k,v) -> sprintf "_%d" v) env)
                    let body = compileP body env  
                    Pccs(name, body))
                (getValuations parameters)  
            
    List.concat (List.map compile vccss)
    
