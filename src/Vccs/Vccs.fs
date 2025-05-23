module Vccs

open Interval

type AExp =
    | Num of int
    | Var of string
    | Add of AExp * AExp
    | Sub of AExp * AExp
    | Mul of AExp * AExp
    | Div of AExp * AExp


type BExp =
    | Eq of AExp * AExp
    | Neq of AExp * AExp
    | More of AExp * AExp
    | Less of AExp * AExp
    | MoreEq of AExp * AExp
    | LessEq of AExp * AExp
    | Not of BExp
    | And of BExp * BExp
    | Or of BExp * BExp

type Action =
    | Input of string * (string * Interval) 
    | Output of string * AExp
    | Silent

type P =
    | Act of Action * P
    | Conditional of BExp * P
    | Sum of P * P
    | Parallel of P * P
    | Restrict of P * string list
    | Rename of P * (Action * Action) list
    | ConstCall of string * AExp list 
    | Nil

type Vccs = string * (string * Interval) list * P



let stringify ((K, expressions, P): Vccs) : string =
    let printInterval (lo, hi) =
        "[" + string lo + ", " + string hi + "]"

    let rec A2S exp =
        match exp with
        | Num n -> string n
        | Var v -> v
        | Add(l, r) -> "(" + A2S l + " + " + A2S r + ")"
        | Sub(l, r) -> "(" + A2S l + " - " + A2S r + ")"
        | Mul(l, r) -> "(" + A2S l + " * " + A2S r + ")"
        | Div(l, r) -> "(" + A2S l + " / " + A2S r + ")"

    let rec B2S bexp =
        match bexp with
        | Eq(l, r) -> "(" + A2S l + " == " + A2S r + ")"
        | Neq(l, r) -> "(" + A2S l + " != " + A2S r + ")"
        | More(l, r) -> "(" + A2S l + " > " + A2S r + ")"
        | MoreEq(l, r) -> "(" + A2S l + " >= " + A2S r + ")"
        | Less(l, r) -> "(" + A2S l + " < " + A2S r + ")"
        | LessEq(l, r) -> "(" + A2S l + " <= " + A2S r + ")"
        | Not b -> "!(" + B2S b + ")"
        | And(l, r) -> "(" + B2S l + " && " + B2S r + ")"
        | Or(l, r) -> "(" + B2S l + " || " + B2S r + ")"

    let rec P2S proc =
        match proc with
        | ConstCall(id, []) -> sprintf "%s" id
        | ConstCall(id, args) -> sprintf "%s(%s)" id (String.concat ", " (List.map A2S args))
        | Act(action, P) ->
            match action with
            | Input(ch, (var, ty)) -> sprintf "%s(%s : %s).%s" ch var (Interval.stringify ty) (P2S P)
            | Output(ch, v) -> "'" + ch + "(" + A2S v + ") . " + P2S P
            | Silent -> "tau . " + P2S P
        | Conditional(b, p) -> "if " + B2S b + " then " + P2S p
        | Sum(p1, p2) -> "(" + P2S p1 + " + " + P2S p2 + ")"
        | Parallel(p1, p2) -> "(" + P2S p1 + " | " + P2S p2 + ")"
        | Restrict(p, names) -> P2S p + " \\ {" + String.concat ", " names + "}"
        | Rename(p, renames) ->
            let R2S rename =
                match rename with
                | Input(x, y) -> sprintf "%s → %s" (Interval.stringify x) y
                | Output(x, y) -> sprintf "%s → %s" x y

            let renStr = renames |> List.map R2S |> String.concat ", "
            P2S p + " [" + renStr + "]"
        | Nil -> "0"

    match expressions with
    | [] -> sprintf "%s = %s" K (P2S P)
    | _ -> sprintf "%s(%s) = %s" K (printExpressions expressions) (P2S P)
