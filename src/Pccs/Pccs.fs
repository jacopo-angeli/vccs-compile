module Pccs

type Pccs =
    | ConstCall         of string                 
    | Input             of string * Pccs          
    | Output            of string * Pccs          
    | Silent            of Pccs                   
    | Sum               of Pccs * Pccs                   
    | Parallel          of Pccs * Pccs               
    | Restrict          of Pccs * string list       
    | Rename            of Pccs * (string * string) list          