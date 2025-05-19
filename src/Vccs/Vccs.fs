module Vccs

type ty = 
    | Interval      of int * int

type AExp =
    | Num           of int
    | Var           of string
    | Add           of AExp * AExp
    | Sub           of AExp * AExp
    | Mul           of AExp * AExp
    | Div           of AExp * AExp


type BExp =
    | Eq            of AExp * AExp
    | Neq           of AExp * AExp
    | More          of AExp * AExp
    | Less          of AExp * AExp
    | MoreEq        of AExp * AExp
    | LessEq        of AExp * AExp
    | Not           of BExp
    | And           of BExp * BExp
    | Or            of BExp * BExp

type Rename =
    | RenameVar of string * string
    | RenameVal of int * string

type P =
    | Input         of string * string * P       
    | Output        of string * AExp * P        
    | Silent        of P                        
    | Conditional   of BExp * P            
    | Sum           of P * P                    
    | Parallel      of P * P               
    | Restrict      of P * string list        
    | Rename        of P * Rename list 
    | ConstCall     of string * AExp list  //P = 'a(x).Z(x+1)     
    | Nil

type Vccs = string * (string * ty) list * P



let stringify ((K, expressions, P) : Vccs) : string =
    let rec A2S exp =
        match exp with
        | Num n              -> string n
        | Var v              -> v
        | Add (l, r)         -> "(" + A2S l + " + " + A2S r + ")"
        | Sub (l, r)         -> "(" + A2S l + " - " + A2S r + ")"
        | Mul (l, r)         -> "(" + A2S l + " * " + A2S r + ")"
        | Div (l, r)         -> "(" + A2S l + " / " + A2S r + ")"

    let rec B2S bexp =
        match bexp with
        | Eq (l, r)          -> "(" + A2S l + " == " + A2S r + ")"
        | Neq (l, r)         -> "(" + A2S l + " != " + A2S r + ")"
        | More (l, r)        -> "(" + A2S l + " > "  + A2S r + ")"
        | MoreEq (l, r)      -> "(" + A2S l + " >= " + A2S r + ")"
        | Less (l, r)        -> "(" + A2S l + " < " + A2S r + ")"
        | LessEq (l, r)      -> "(" + A2S l + " <= " + A2S r + ")"
        | Not b              -> "!(" + B2S b + ")"
        | And (l, r)         -> "(" + B2S l + " && " + B2S r + ")"
        | Or (l, r)          -> "(" + B2S l + " || " + B2S r + ")"

    let rec P2S proc =
        match proc with    
        | ConstCall (id, []) ->
            sprintf "%s" id 
        | ConstCall (id, args) ->
            sprintf "%s(%s)" id (String.concat ", " (List.map A2S args)) 
        | Input (ch, x, p) ->
            ch + "(" + x + ") . " + P2S p
        | Output (ch, v, p) ->
            "'"+ ch + "("+ A2S v + ") . " + P2S p
        | Silent p ->
            "tau . " + P2S p
        | Conditional (b, p) ->
            "if " + B2S b + " then " + P2S p
        | Sum (p1, p2) ->
            "(" + P2S p1 + " + " + P2S p2 + ")"
        | Parallel (p1, p2) ->
            "(" + P2S p1 + " | " + P2S p2 + ")"
        | Restrict (p, names) ->
            P2S p + " \\ {" + String.concat ", " names + "}"
        | Rename (p, renames) ->
            let R2S rename = 
                match rename with
                | RenameVal(x,y) -> sprintf "%d → %s" x y
                | RenameVar(x,y) -> sprintf "%s → %s" x y

            let renStr = renames |> List.map R2S |> String.concat ", "
            P2S p + " [" + renStr + "]"
        | Nil ->
            "0"


    let printTy ty =
        match ty with
        | Interval (lo, hi) -> "[" + string lo + ", " + string hi + "]"

    let printExpressions (pars : (string * ty) list) =
        pars
        |> List.map (fun (name, ty) -> name + " : " + printTy ty)
        |> String.concat ", "

    match expressions with
    | [] -> sprintf "%s = %s" K (P2S P)
    | _ -> sprintf "%s(%s) = %s" K (printExpressions expressions) (P2S P)