﻿namespace FsControl.Core.TypeMethods

open FsControl.Core
open FsControl.Core.Prelude
open FsControl.Core.Types
open FsControl.Core.TypeMethods.Monad


type Traverse() =
    inherit Default1()
    static member val Instance = Traverse()

    static member inline Traverse (t:Id<_>, f, _, _:Default1) = Map.Invoke Id.create (f (Id.run t))
    static member inline Traverse (t:_ seq, f, _, _:Default1) = 
        let cons x y = Seq.append (Seq.singleton x) y            
        let cons_f x ys = Map.Invoke cons (f x) <*> ys
        Foldr.Invoke cons_f (result (Seq.empty)) t

    static member Traverse (t:_ seq ,f , _:option<seq<_>>, _:Traverse) =
        let ok = ref true
        let res = Seq.toArray (seq {
                use e = t.GetEnumerator()
                while (e.MoveNext() && ok.Value) do
                    match f e.Current with
                    | Some v -> yield v
                    | None   -> ok.Value <- false})
        if ok.Value then Some (Array.toSeq res) else None

    static member Traverse (t:Id<_>    ,f , _:option<Id<_>>, _:Traverse) = Option.map Id.create (f (Id.run t))
  
    static member inline Traverse (t:option<_>,f , _, _:Traverse) = match t with Some x -> Map.Invoke Some (f x) | _ -> result None        

    static member inline Traverse (t:list<_>  ,f , _, _:Traverse) =         
        let cons_f x ys = Map.Invoke List.cons (f x) <*> ys
        Foldr.Invoke cons_f (result []) t

    static member inline Traverse (t:_ []  ,f , _, _:Traverse) =
        let cons x y = Array.append [|x|] y            
        let cons_f x ys = Map.Invoke cons (f x) <*> ys
        Foldr.Invoke cons_f (result [||]) t

    static member inline Invoke f t =
        let inline call_3 (a:^a, b:^b, c:^c, f) = ((^a or ^b or ^c) : (static member Traverse: _*_*_*_ -> _) b, f, c, a)
        let inline call (a:'a, b:'b, f) = call_3 (a, b, Unchecked.defaultof<'r>, f) :'r
        call (Traverse.Instance, t, f)
    

type SequenceA() =
    inherit Default1()
    static member val Instance = SequenceA()
    static member inline SequenceA (t          , _, _:Default1 ) = Traverse.Invoke id t
    static member inline SequenceA (t:option<_>, _, _:SequenceA) = match t with Some x -> Map.Invoke Some x | _ -> result None       
    static member inline SequenceA (t:list<_>  , _, _:SequenceA) = let cons_f x ys = Map.Invoke List.cons x <*> ys in Foldr.Invoke cons_f (result []) t
    static member inline SequenceA (t:seq<_>   , _, _:SequenceA) = Traverse.Invoke id t
    static member inline SequenceA (t:Id<_>    , _, _:SequenceA) = Traverse.Invoke id t

    static member inline Invoke (t:'Traversable'Applicative'T) :'Applicative'Traversable'T =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member SequenceA: _*_*_ -> _) b, c, a)
        let inline call (a:'a, b:'b) = call_3 (a, b, Unchecked.defaultof<'r>) :'r
        call (SequenceA.Instance, t)