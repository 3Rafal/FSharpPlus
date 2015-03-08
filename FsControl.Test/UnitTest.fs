﻿namespace FsControl.Test

open System
open Microsoft.VisualStudio.TestTools.UnitTesting
open FsControl.Operators

module Combinators =
    let inline flip f x y = f y x
    let inline konst k _ = k
    let inline (</) x = (|>) x
    let inline (/>) x = flip x
    let inline choice f g = function Choice2Of2 x -> f x | Choice1Of2 y -> g y
    let inline option n f = function None -> n | Some x -> f x

open Combinators





type WrappedListA<'s> = WrappedListA of 's list with
    static member ToSeq    (WrappedListA lst) = List.toSeq lst
    static member FromSeq  lst = WrappedListA (Seq.toList lst)

[<TestClass>]
type Foldable() = 
    [<TestMethod>]
    member x.filter_Default_Custom() = 
        let wlA1 = WrappedListA [1..10]
        let testVal = filter ((=)2) wlA1
        Assert.AreEqual (testVal, WrappedListA [2])
        Assert.IsInstanceOfType(Some testVal, typeof<Option<WrappedListA<int>>>)

    [<TestMethod>]
    member x.FromToSeq() =
        let s = (seq [Collections.Generic.KeyValuePair(1, "One"); Collections.Generic.KeyValuePair(2, "Two")])
        let dc2:Collections.Generic.Dictionary<_,_>   = fromSeq s
        let s' = toSeq s
        Assert.AreEqual (s, s')
        Assert.IsInstanceOfType(Some s, (Some s').GetType())

[<TestClass>]
type Traversable() = 
    [<TestMethod>]
    member x.sequenceA_Default_Primitive() = 
        let testVal = sequenceA [|Some 1; Some 2|]
        Assert.AreEqual (Some [|1;2|], testVal)
        Assert.IsInstanceOfType (testVal, typeof<Option<array<int>>>)

        
type ZipList<'s> = ZipList of 's seq with
    static member Map    (ZipList x, f:'a->'b)               = ZipList (Seq.map f x)
    static member Return (x:'a)                              = ZipList (Seq.initInfinite (konst x))
    static member (<*>) (ZipList (f:seq<'a->'b>), ZipList x) = ZipList (Seq.zip f x |> Seq.map (fun (f, x) -> f x)) :ZipList<'b>
    


[<TestClass>]
type Applicative() = 
    [<TestMethod>]
    member x.ApplicativeMath() = 
        let inline (+) (a:'T) (b:'T) :'T = a + b
        let inline ( |+  ) (x :'Functor't)     (y :'t)             = map ((+)/> y) x :'Functor't
        let inline (  +| ) (x :'t)             (y :'Functor't)     = map ((+)   x) y :'Functor't
        let inline ( |+| ) (x :'Applicative't) (y :'Applicative't) = (+) <!> x <*> y :'Applicative't

        let testVal = [1;2] |+| [10;20] |+| [100;200] |+  2
        Assert.AreEqual ([113; 213; 123; 223; 114; 214; 124; 224], testVal)
        Assert.IsInstanceOfType (Some testVal, typeof<Option<list<int>>>)


    [<TestMethod>]
    member x.Applicatives() = 

        let run (ZipList x) = x

        // Test Applicative (functions)
        let res607 = map (+) ( (*) 100 ) 6 7
        let res606 = ( (+) <*>  (*) 100 ) 6
        let res508 = (map (+) ((+) 3 ) <*> (*) 100) 5

        // Test Applicative (ZipList)
        let res9n5   = map ((+) 1) (ZipList [8;4])
        let res20n30 = result (+) <*> result 10 <*> ZipList [10;20]
        let res18n14 = result (+) <*> ZipList [8;4] <*> result 10

        Assert.AreEqual (607, res607)
        Assert.AreEqual (606, res606)
        Assert.AreEqual (508, res508)


    // Idiom brackets from http://www.haskell.org/haskellwiki/Idiom_brackets
    type Ii = Ii
    type Ji = Ji
    type J = J
    type Idiomatic = Idiomatic with
        static member inline ($) (Idiomatic, si) = fun sfi x -> (Idiomatic $ x) (sfi <*> si)
        static member        ($) (Idiomatic, Ii) = id

type Applicative with
    [<TestMethod>]
    member x.IdiomBrackets() =    
        let inline idiomatic a b = (Idiomatic $ b) a
        let inline iI x = (idiomatic << result) x

        let res3n4''  = iI ((+) 2) [1;2] Ii
        let res3n4''' = iI (+) (result 2) [1;2] Ii   // fails to compile when constraints are not properly defined
        Assert.AreEqual ([3;4], res3n4'' )
        Assert.AreEqual ([3;4], res3n4''')




        let output = System.Text.StringBuilder()
        let append (x:string) = output.Append x |> ignore

        let v5: Lazy<_> = lazy (append "5"; 5)
        Assert.AreEqual (0, output.Length)
        let fPlus10 x   = lazy (append " + 10"; x + 10)
        Assert.AreEqual (0, output.Length)
        let v5plus10    = v5 >>= fPlus10
        Assert.AreEqual (0, output.Length)
        let v15 = v5plus10.Force()
        Assert.AreEqual ("5 + 10", output.ToString())
        Assert.AreEqual (15, v15)

        output.Clear() |> ignore

        let v4ll: Lazy<_> = lazy (append "outer"; lazy (append "inner"; 4))
        Assert.AreEqual (0, output.Length)
        let v4l = join v4ll
        Assert.AreEqual (0, output.Length)
        let v4  = v4l.Force()
        Assert.AreEqual ("outerinner", output.ToString())
        Assert.AreEqual (4, v4)
 

open FsControl.Core.Types
        
[<TestClass>]
type MonadTransformers() = 
    [<TestMethod>]
    member x.Lift_N_layersMonadTransformer() = 
        let getLine    = async { return System.Console.ReadLine() }

        let resLiftIOErrorT = liftAsync getLine : ErrorT<Async<Choice<_,string>>>
        let res3Layers   = (lift << lift)         getLine : OptionT<ReaderT<string,_>>
        let res3Layers'  = (lift << lift)         getLine : OptionT<WriterT<Async<_ * string>>>
        let res3Layers'' = liftAsync              getLine : OptionT<WriterT<Async<_ * string>>>
        let res4Layers'  = liftAsync              getLine : ListT<OptionT<WriterT<Async<_ * string>>>>
        let res4Layers   = (lift << lift << lift) getLine : ListT<OptionT<WriterT<Async<_ * string>>>>

        Assert.IsInstanceOfType (Some resLiftIOErrorT, typeof<Option<ErrorT<Async<Choice<string,string>>>>>)
        Assert.IsInstanceOfType (Some res3Layers  , typeof<Option<OptionT<ReaderT<string, Async<string option>>>>>)
        Assert.IsInstanceOfType (Some res3Layers' , typeof<Option<OptionT<WriterT<Async<string option * string>>>>>)
        Assert.IsInstanceOfType (Some res3Layers'', typeof<Option<OptionT<WriterT<Async<string option * string>>>>>)
        Assert.IsInstanceOfType (Some res4Layers' , typeof<Option<ListT<OptionT<WriterT<Async<string list option * string>>>>>>)
        Assert.IsInstanceOfType (Some res4Layers  , typeof<Option<ListT<OptionT<WriterT<Async<string list option * string>>>>>>)