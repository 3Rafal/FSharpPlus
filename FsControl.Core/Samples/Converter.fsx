﻿#r @"..\bin\Release\FsControl.Core.dll"

open System
open FsControl.Core.TypeMethods.Converter

let inline fromBytesWithOffset (isLtEndian:bool) (startIndex:int) (value:byte[]) = Inline.instance FromBytes (value, startIndex, isLtEndian)
let inline fromBytes           (isLtEndian:bool)                  (value:byte[]) = Inline.instance FromBytes (value, 0         , isLtEndian)
let inline toBytes             (isLtEndian:bool) value :byte[] = Inline.instance (ToBytes, value) isLtEndian
let inline toString  value:string  = Inline.instance (ToString, value) Globalization.CultureInfo.InvariantCulture
let inline tryParse (value:string) = Inline.instance TryParse value
let inline parse    (value:string) = Inline.instance Parse    value

let r101 = tryParse "10.1.0.1" : Net.IPAddress option
let r102 = tryParse "102" : string option
let rMTS = [tryParse "Monday" ; Some DayOfWeek.Thursday; Some DayOfWeek.Saturday]
let r103 = tryParse "103" : Text.StringBuilder option

let r111 = parse "true" && true
let rMTF = [parse "Monday" ; DayOfWeek.Thursday; DayOfWeek.Friday]
let r110 = parse "10" + fromBytes true [|10uy;0uy;0uy;0uy;0uy;0uy;0uy;0uy|] + 100.
let r120 = parse "10" + fromBytes true [|10uy;0uy;0uy;0uy;|]                + 100
let r121 = parse "121" : string
let r122 = parse "122" : Text.StringBuilder

let r123 = toString [1;2;3]
let r140 = toString (1,4,0)
let r150 = toString (Some 150)
let r160 = toString ([1;6;0] :> _ seq)
let r170 = toString (ResizeArray([1;7;0]))
let r180 = toString (Set [1;8;0])
let r190 = toString [|1;9;0|]
let r200 = toString [|([1;2;3] :> _ seq);([4;5;6] :> _ seq);([7;8;9] :> _ seq)|]
let r210 = toString (Map  ['a',2; 'b',1; 'c',0])
let r220 = toString (dict ['a',2; 'b',2; 'c',0])


// Generic op_Explicit
let r301:string = convert 301
let r302:float  = convert 302
let r303:float  = convert "303"
let r304:char   = convert "F"