module Interval

type Interval = int * int

let stringify ((lo, hi):Interval) =
    if lo = hi then sprintf "%d" lo else sprintf "[%d,%d]" lo hi
