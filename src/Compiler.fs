module Compiler

open Vccs
open Pccs
open Interval

let compile (vccs_proc_list: Vccs list) : Pccs list =

    let getValuations (parameters: (string * Interval) list) : (string * int) list list =
        let domains =
            parameters |> List.map (fun (name, (lo, hi)) -> name, [lo .. hi])
        let rec cartesianProduct domains =
            match domains with
            | [] -> [ [] ]
            | (name, values) :: rest ->
                [ for value in values do
                    for combo in cartesianProduct rest ->
                        (name, value) :: combo ]
        cartesianProduct domains

    let rec compileDecl (vccs_proc: Vccs) : Pccs list =
        
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
                    let values = Interval.toList interval
                    if values = [] then
                        failwithf "Empty interval for input channel '%s'" channel
                    values
                    |> List.map (fun value ->
                            Pccs.Act(Pccs.Input (sprintf "%s_%d" channel value), compileP P ((var, value) :: env)))
                    |> List.reduce (fun x y -> Pccs.Sum(x,y))

                | Vccs.Output(channel, expr) ->
                    Pccs.Act(Pccs.Output (sprintf "%s_%d" channel (evaluateA expr)), compileP P env)

            | Vccs.Conditional(bExp, thenP, elseP) ->
                if evaluateB bExp then compileP thenP env else compileP elseP env

            | Vccs.Sum(P, P')-> Pccs.Sum(compileP P env, compileP P' env)
            | Vccs.Parallel(P,P')-> Pccs.Parallel(compileP P env, compileP P' env)
            | Vccs.Restrict(P, typedChannels) ->
                let expandedNames =
                    typedChannels
                    |> List.collect (fun (ch, interval) ->
                        Interval.toList interval |> List.map (fun v -> sprintf "%s_%d" ch v))
                Pccs.Restrict(compileP P env, expandedNames)
            | Vccs.Rename(P, f) ->
                let expandedRenames =
                    f |> List.collect (fun (fromCh, toCh, interval) ->
                        Interval.toList interval
                        |> List.collect (fun v ->
                            [ (Pccs.Input  (sprintf "%s_%d" fromCh v), Pccs.Input  (sprintf "%s_%d" toCh v))
                              (Pccs.Output (sprintf "%s_%d" fromCh v), Pccs.Output (sprintf "%s_%d" toCh v)) ]))
                Pccs.Rename(compileP P env, expandedRenames)

            | Vccs.ConstCall (K, []) ->
                Pccs.ConstCall K

            | Vccs.ConstCall (K, expressions) ->
                let evals = List.map evaluateA expressions
                let name = K + (evals |> List.map (sprintf "_%d") |> String.concat "")
                Pccs.ConstCall name

            | Vccs.Nil -> Pccs.Nil       
        
        match vccs_proc with 
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
            
    vccs_proc_list |> List.collect compileDecl
    
