module Lexer

open FSharp.Text.Lexing
open Parser/// Rule token
val token: lexbuf: LexBuffer<char> -> token
