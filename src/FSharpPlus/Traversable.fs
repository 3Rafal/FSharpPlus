﻿namespace FsControl

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open FsControl.Internals
open FsControl.Internals.Prelude
open FsControl.Internals.MonadOps
open FSharpPlus


type Traverse =
    inherit Default1
    static member inline Traverse (t:^a   , f, [<Optional>]_output:'R, [<Optional>]_impl:Default4) = Map.Invoke f (^a : (static member SequenceA: _ -> 'R) t)
    static member inline Traverse (t:Id<_>, f, [<Optional>]_output:'R, [<Optional>]_impl:Default3) = Map.Invoke Id.create (f (Id.run t))
    static member inline Traverse (t:_ seq, f, [<Optional>]_output:'R, [<Optional>]_impl:Default3) =
       let cons x y = seq {yield x; yield! y}
       let cons_f x ys = Map.Invoke (cons:'a->seq<_>->seq<_>) (f x) <*> ys
       Seq.foldBack cons_f t (result (Seq.empty))

    static member Traverse (t:'t seq ,f:'t->'u option , [<Optional>]_output:option<seq<'u>>, [<Optional>]_impl:Default2) =
       let ok = ref true
       let res = Seq.toArray (seq {
           use e = t.GetEnumerator()
           while (e.MoveNext() && ok.Value) do
               match f e.Current with
               | Some v -> yield v
               | None   -> ok.Value <- false})
       if ok.Value then Some (Array.toSeq res) else None

    static member        Traverse (t:'t seq   ,f:'t->Async<'u> , [<Optional>]_output:Async<seq<'u>>, [<Optional>]_impl:Default2) :Async<seq<_>> = result <| Seq.map (Async.RunSynchronously) (Seq.map f t)

    static member inline Traverse (t:^a   , f, [<Optional>]_output:'R, [<Optional>]_impl:Default1) = (^a : (static member Traverse: _*_ -> 'R) t, f)
    static member inline Traverse (_:^a when ^a : null and ^a :struct, _, _:'R   , _impl:Default1) = id

    static member        Traverse (t:Id<'t>   ,f:'t->option<'u>, [<Optional>]_output:option<Id<'u>>, [<Optional>]_impl:Traverse) = Option.map Id.create (f (Id.run t))
    static member inline Traverse (t:option<_>,f , [<Optional>]_output:'R, [<Optional>]_impl:Traverse) :'R = match t with Some x -> Map.Invoke Some (f x) | _ -> result None        

    static member inline Traverse (t:list<_>  ,f , [<Optional>]_output:'R, [<Optional>]_impl:Traverse) :'R =         
       let cons_f x ys = Map.Invoke List.cons (f x) <*> ys
       List.foldBack cons_f t (result [])

    static member inline Traverse (t:_ []  ,f , [<Optional>]_output :'R  , [<Optional>]_impl:Traverse) :'R =
       let cons x y = Array.append [|x|] y            
       let cons_f x ys = Map.Invoke cons (f x) <*> ys
       Array.foldBack cons_f t (result [||])

    static member inline Invoke f t =
        let inline call_3 (a:^a, b:^b, c:^c, f) = ((^a or ^b or ^c) : (static member Traverse: _*_*_*_ -> _) b, f, c, a)
        let inline call (a:'a, b:'b, f) = call_3 (a, b, Unchecked.defaultof<'R>, f) :'R
        call (Unchecked.defaultof<Traverse>, t, f)
    

[<Extension;Sealed>]
type SequenceA =
    inherit Default1

    [<Extension>]static member inline SequenceA (t:_ seq         , [<Optional>]_output:'R, [<Optional>]_impl:Default3 ) :'R =                                                                                         
                        let cons x y = seq {yield x; yield! y}
                        let cons_f x ys = Map.Invoke (cons:'a->seq<_>->seq<_>) x <*> ys
                        Seq.foldBack cons_f t (result Seq.empty)

    [<Extension>]static member inline SequenceA (t:^a            , [<Optional>]_output:'R, [<Optional>]_impl:Default2 ) = (^a : (static member Traverse: _*_ -> 'R) t, id)                                     :'R
    [<Extension>]static member inline SequenceA (t:^a            , [<Optional>]_output:'R, [<Optional>]_impl:Default1 ) = (^a : (static member SequenceA: _ -> 'R) t)                                          :'R
    [<Extension>]static member inline SequenceA (t:option<_>     , [<Optional>]_output:'R, [<Optional>]_impl:SequenceA) = match t with Some x -> Map.Invoke Some x | _ -> result None                             :'R
    [<Extension>]static member inline SequenceA (t:list<_>       , [<Optional>]_output:'R, [<Optional>]_impl:SequenceA) = let cons_f x ys = Map.Invoke List.cons x <*> ys in List.foldBack cons_f t (result [])   :'R
    [<Extension>]static member inline SequenceA (t:_ []          , [<Optional>]_output:'R, [<Optional>]_impl:SequenceA) = let cons x y = Array.append [|x|] y in let cons_f x ys = Map.Invoke cons x <*> ys in Array.foldBack cons_f t (result [||]) :'R
    [<Extension>]static member inline SequenceA (t:Id<_>         , [<Optional>]_output:'R, [<Optional>]_impl:SequenceA) = Traverse.Invoke id t                                                                    :'R
    [<Extension>]static member inline SequenceA (t: _ ResizeArray, [<Optional>]_output:'R, [<Optional>]_impl:SequenceA) = Traverse.Invoke id t                                                                    :'R

    static member inline Invoke (t:'Traversable'Applicative'T) :'Applicative'Traversable'T =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member SequenceA: _*_*_ -> _) b, c, a)
        let inline call (a:'a, b:'b) = call_3 (a, b, Unchecked.defaultof<'R>) :'R
        call (Unchecked.defaultof<SequenceA>, t)