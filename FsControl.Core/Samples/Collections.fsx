﻿#r @"..\bin\Release\FsControl.Core.dll"

open System
open FsControl.Core.TypeMethods
open FsControl.Core.TypeMethods.Collection
open FsControl.Core.TypeMethods.Applicative
open FsControl.Core.TypeMethods.Comonad
open FsControl.Core.TypeMethods.Foldable
open FsControl.Core.TypeMethods.Monoid
open FsControl.Operators

let flip f x y = f y x
let konst k _ = k
let (</) = (|>)
let (/>) = flip

type ZipList<'s> = ZipList of 's seq with
    static member Return (x:'a)                              = ZipList (Seq.initInfinite (konst x))
    static member (<!>) (f:'a->'b,  ZipList x)               = ZipList (Seq.map f x)
    static member (<*>) (ZipList (f:seq<'a->'b>), ZipList x) = ZipList (Seq.zip f x |> Seq.map (fun (f,x) -> f x)) :ZipList<'b>
    static member inline Mempty() = result (mempty())                                :ZipList<'a>
    static member inline Mappend (x:ZipList<'a>, y:ZipList<'a>) = liftA2 mappend x y :ZipList<'a>

    // try also commenting/uncommenting the following method.
    static member inline Mconcat (x:list<ZipList<'a>>) = printfn "ZipList mconcat optimized"; List.foldBack mappend x (mempty()):ZipList<'a>

type WrappedList<'s> = WrappedList of 's list with
    static member instance (_:Applicative.Pure, _:WrappedList<'a>) = fun (x:'a)     -> WrappedList [x]
    static member instance (_:Monoid.Mappend, WrappedList l, _) = fun (WrappedList x) -> WrappedList (l @ x)
    static member instance (_:Monoid.Mempty  , _:WrappedList<'a>   ) = fun () -> WrappedList List.empty
    static member instance (_:Foldable.Foldr, WrappedList x, _) = fun (f,z) -> List.foldBack f x z

let wl = WrappedList  [2..10]

let threes = filter ((=) 3) [ 1;2;3;4;5;6;1;2;3;4;5;6 ]
let fours  = filter ((=) 4) [|1;2;3;4;5;6;1;2;3;4;5;6|]
let five   = filter ((=) 5) (WrappedList [1;2;3;4;5;6])   // <- Uses the default method for filter.
let optionFilter = filter ((=) 3) (Some 4)

let arrayGroup = groupBy ((%)/> 2) [|11;2;3;9;5;6;7;8;9;10|]
let listGroup  = groupBy ((%)/> 2) [ 11;2;3;9;5;6;7;8;9;10 ]
let seqGroup   = groupBy ((%)/> 2) (seq [11;2;3;9;5;6;7;8;9;10])

let arrayGroupAdj   = groupAdjBy ((%)/> 2) [11;2;3;9;5;6;7;8;9;10]

let sortedList = sortBy string     [ 11;2;3;9;5;6;7;8;9;10 ]
let sortedSeq  = sortBy string (seq [11;2;3;9;5;6;7;8;9;10])

let bigSeq = seq {1..10000000}
let bigLst = [ 1..10000000 ]
let bigArr = [|1..10000000|]
let bigMut = ResizeArray(seq {1..10000000})

let x = extract bigSeq
let y = extract bigLst
let z = extract bigArr

let a = skip 1000 bigSeq
let b = skip 1000 bigLst
let c = skip 1000 bigArr
let d = skip 1000 bigMut
let e = "hello world" |> skip 6 |> toList
let h = fromList ['h';'e';'l';'l';'o';' '] + "world"


// Monoids

let asQuotation = mappend <@ ResizeArray(["1"]) @> <@ ResizeArray(["2;3"]) @>
let quot123     = mappend <@ ResizeArray([1])   @> <@ ResizeArray([2;3])   @>
let quot1       = mappend <@ ResizeArray([1])   @>      (mempty())
let quot23      = mappend    (mempty())            <@ ResizeArray([2;3])   @>
let quot13      = mappend    (mempty())            <@ ("1","3") @>
let quotLst123  = mappend    (mempty())            (ZipList [ [1];[2];[3] ])
let quotLst123' = mconcat    [mempty();  mempty();  ZipList [ [1];[2];[3] ]]

let lzy1 = mappend (lazy [1]) (lazy [2;3])
let lzy2 = mappend (mempty()) lzy1
let asy1 = mappend (async.Return [1]) (async.Return [2;3])
let asy2 = mappend (mempty()) asy1

let mapA = Map.empty 
            |> Map.add 1 (async.Return "Hey")
            |> Map.add 2 (async.Return "Hello")

let mapB = Map.empty 
            |> Map.add 3 (async.Return " You")
            |> Map.add 2 (async.Return " World")

let mapAB = mappend mapA mapB
let greeting1 = Async.RunSynchronously mapAB.[2]
let greeting2 = Async.RunSynchronously (mconcat [mapA; mempty(); mapB]).[2]

open System.Collections.Generic
open System.Threading.Tasks

let dicA = new Dictionary<string,Task<string>>()
dicA.["keya"] <- (result "Hey"  : Task<_>)
dicA.["keyb"] <- (result "Hello": Task<_>)

let dicB = new Dictionary<string,Task<string>>()
dicB.["keyc"] <- (result " You"  : Task<_>)
dicB.["keyb"] <- (result " World": Task<_>)

let dicAB = mappend dicA dicB

let greeting3 = extract dicAB.["keyb"]
let greeting4 = extract (mconcat [dicA; mempty(); dicB]).["keyb"]

let res2 = mconcat [ async {return (+) 2 } ; async {return (*) 10 } ; async {return id } ;  async {return (%) 3 } ; async {return mempty() } ] </Async.RunSynchronously/> 3


// Functors, Monads

let quot7 = map ((+)2) <@ 5 @>
let (quot5:Microsoft.FSharp.Quotations.Expr<int>) = result 5

// Do notation
type MonadBuilder() =
    member inline b.Return(x)    = result x
    member inline b.Bind(p,rest) = p >>= rest
    member        b.Let (p,rest) = rest p
    member    b.ReturnFrom(expr) = expr

let monad     = new MonadBuilder()



// Seq

let stack = new Collections.Generic.Stack<_>([1;2;3])

let twoSeqs = mappend (seq [1;2;3]) (seq [4;5;6])
let sameSeq = mappend (mempty()) (seq [4;5;6])

let seqFromLst:_ seq = fromList [1;2;3;4]
let seqFromLst' = toSeq [1;2;3;4]
let seqFromOpt  = toSeq (Some 1)

// This should not compile 
(*
let twoStacks = mappend stack stack
let twoSeqs'  = mappend (seq [1;2;3]) [4;5;6]
let twoSeqs'' = mappend [1;2;3] (seq [4;5;6])
let (stackFromLst:_ Collections.Generic.Stack) = fromList [1;2;3;4]
*)

let singletonList: _ list = result 1
let singletonSeq : _ seq  = result 1


// This should not compile (but it does)
let sortedStack = sortBy  string    stack
let mappedstack = map string stack
let stackGroup  = groupBy ((%)/> 2) stack


// Test Seq Monad
                        
let rseq =
    monad {
        let! x1 =  [1;2]
        let! x2 = seq [10;20]
        return ((+) x1 x2) }


// Test Seq Comonad

let lst   = seq [1;2;3;4;5]
let elem1 = extract   lst
let tails = duplicate lst
let lst'  = extend extract lst


// Some problems associated with this approach in a language with inheritance.

// This should ideally not compile (but it does, because stack is also a seq)
let elem3  = extract   stack
let tails' = duplicate stack

// This should not compile
// let stk'  = extend extract stack


// Test foldable

let r10  = foldr (+) 0 (seq [1;2;3;4])
let r323 = toList (seq [3;2;3])
let r03  = filter ((=) 3) (seq [1;2;3])

// This should not compile ??? (but it does)
let r10' = foldr (+) 0 stack
let r123 = toList stack

// This should not compile
// let r03' = filter ((=) 3) stack


// Test traversable

let resNone  = sequenceA (seq [Some 3;None ;Some 1])

// This should not compile (but it does)
let resNone' = sequenceA (new Collections.Generic.Stack<_>([Some 3;None  ;Some 1]))



let getLine    = async { return System.Console.ReadLine() }
let putStrLn x = async { printfn "%s" x}

let inline sequence ms =
    let k m m' = m >>= fun (x:'a) -> m' >>= fun xs -> (result :seq<'a> -> 'M) (Seq.append (Seq.singleton x) xs)
    List.foldBack k (Seq.toList ms) ((result :seq<'a> -> 'M) (Seq.empty))

let inline mapM f as' = sequence (Seq.map f as')

type DoPlusNotationBuilder() =
    member inline b.Return(x) = result x
    member inline b.Bind(p,rest) = p >>= rest
    member b.Let(p,rest) = rest p
    member b.ReturnFrom(expr) = expr
    member inline x.Zero() = zero()
    member inline x.Combine(a, b) = a <|> b
let doPlus = new DoPlusNotationBuilder()

// Test MonadPlus
let nameAndAddress = mapM (fun x -> putStrLn x >>= fun _ -> getLine) (seq ["name";"address"])

// this compiles but it requires a type annotation to tell between
// seq and other monadplus #seq types
let pythags = monad {
  let! z = seq [1..50]
  let! x = seq [1..z]
  let! y = seq [x..z]
  do! (guard (x*x + y*y = z*z) : _ seq)
  return (x, y, z)}


let pythags' = doPlus{
  let! z = seq [1..50]
  let! x = seq [1..z]
  let! y = seq [x..z]
  if (x*x + y*y = z*z) then return (x, y, z)}

let res123123 = (seq [1;2;3]) <|> (seq [1;2;3])
let allCombinations = sequence (seq [seq ['a';'b';'c']; seq ['1';'2']]) //|> Seq.map Seq.toList |> Seq.toList 



// Test SeqT Monad Transformer

open FsControl.Core.Types

let listT  = ListT (Some [2;4]      ) >>= fun x -> ListT (Some [x; x+10]      )
let seqT   = SeqT  (Some (seq [2;4])) >>= fun x -> SeqT  (Some (seq [x; x+10]))
let resListTSome2547 = (SeqT (Some (seq [2;4]) )) >>=  (fun x -> SeqT ( Some (seq [x;x+3])) )
let apSeqT  = SeqT.run ((SeqT  (Some [(+) 3]) ) <*> ( SeqT  (Some [3]) ))