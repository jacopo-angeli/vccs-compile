module TestGenerateParameterValuations

open System

type Interval = int * int

/// Generates all possible concrete valuations for a given list of typed parameters.
/// Each valuation is a list of (parameterName, value) pairs.
let generateParameterValuations (parameters: (string * Interval) list) : (string * int) list list =
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

/// Pretty printer for valuations
let formatValuation (valuation: (string * int) list) : string =
    valuation
    |> List.map (fun (name, value) -> sprintf "%s = %d" name value)
    |> String.concat ", "
    |> sprintf "[%s]"

let tests = [
    ("Empty input", []);
    ("Single param", [("x", (0, 0))]);
    ("Two parameters", [("x", (0, 1)); ("y", (1, 2))]);
    ("Three parameters", [("a", (1,1)); ("b", (0,1)); ("c", (1,5))])
]
let run = 
    for (name, parameters) in tests do
        let valuations = generateParameterValuations parameters
        printfn $"Test: {name}"
        printfn $"parameters: %A{parameters}"
        printfn $"Generated %d{List.length valuations} valuations:"
        valuations |> List.iter (fun v -> printfn $"  %s{formatValuation v}")
        printfn ""
run
