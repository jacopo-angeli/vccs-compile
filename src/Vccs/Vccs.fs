module Vccs

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
    | Gt            of AExp * AExp
    | Le            of AExp * AExp
    | Ge            of AExp * AExp
    | Not           of BExp
    | And           of BExp * BExp
    | Or            of BExp * BExp


type Vccs =
    | ConstCall     of string * AExp list       
    | Input         of string * string * Vccs       
    | Output        of string * AExp * Vccs        
    | Silent        of Vccs                        
    | Conditional   of BExp * Vccs            
    | Sum           of Vccs * Vccs                    
    | Parallel      of Vccs * Vccs               
    | Restrict      of Vccs * string list        
    | Rename        of Vccs * (string * string) list 
