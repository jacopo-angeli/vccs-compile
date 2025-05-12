module PccsPrinter

open Pccs

let rec printPccs proc =
    match proc with
    | ConstCall name -> name
    | Input (ch, proc) -> ch + "." + printPccs proc
    | Output (ch, proc) -> "'" + ch + "." + printPccs proc
    | Silent proc -> "τ." + printPccs proc
    | Sum (proc1, proc2) -> printPccs proc1 + " + " + printPccs proc2
    | Parallel (proc1, proc2) -> printPccs proc1 + " | " + printPccs proc2
    | Restrict (proc, names) -> printPccs proc + " \\ {" + String.concat ", " names + "}"
    | Rename (p, renames) -> printPccs p + "[" + List.fold (fun acc s -> if acc = "" then s else acc + ", " + s) "" (List.map (fun (x,y) -> x + "/ " + y) renames) + "]"

let example =
    Rename(
        Parallel(
            Input("a", ConstCall "P"),
            Output("b", Silent (ConstCall "Q"))
        ),
        [ ("a", "c"); ("b", "d") ]
    )

printfn "%s" (printPccs example)