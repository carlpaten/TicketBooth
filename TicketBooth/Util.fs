[<AutoOpen>]
module Util

open System
open System.Collections.Generic
open System.Text.RegularExpressions

let constant a b = a

let impossible () = failwith "impossible"

[<Obsolete>]
let TODO x = failwith "TODO"

let memoize f =
    let cache = Dictionary<_, _>()
    fun x ->
        if cache.ContainsKey(x) then cache.[x]
        else let res = f x
             cache.[x] <- res
             res

/// Key-Value Pair recognizer            
let (|KVP|) (kvp: KeyValuePair<_, _>) = (kvp.Key, kvp.Value)

let (|RegexContains|_|) pattern input =
    let m = Regex.Matches(input, pattern)
    match m.Count with
    | 0 -> None
    | n -> 
        match m.[0].Groups.Count with
        | 1 -> Some m.[0].Groups.[0].Value
        | n -> Some m.[0].Groups.[1].Value

let regex_find pattern input =
    let m = Regex.Match(input, pattern)
    match m.Groups.Count with
    | 0 -> impossible ()
    | 1 -> m.Groups.[0].Value
    | n -> m.Groups.[1].Value

module Map =
    let keys m = Map.fold (fun (s: Set<_>) k v -> s.Add k) Set.empty m