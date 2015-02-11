﻿namespace FsControl.Core.TypeMethods

open FsControl.Core
open FsControl.Core.Types
open System
open System.Text

module Collection =

    type Skip = Skip with
        static member instance (Skip, x:string        , _:string        ) = fun n -> x.[n..]
        static member instance (Skip, x:StringBuilder , _:StringBuilder ) = fun n -> new StringBuilder(x.ToString().[n..])
        static member instance (Skip, x:'a []         , _:'a []         ) = fun n -> x.[n..] : 'a []
        static member instance (Skip, x:'a ResizeArray, _:'a ResizeArray) = fun n -> ResizeArray<'a> (Seq.skip n x)
        static member instance (Skip, x:list<'a>      , _:list<'a>      ) =
            let rec listSkip lst = function 0 -> lst | n -> listSkip (List.tail lst) (n-1) 
            listSkip x
        static member instance (Skip, x:seq<'a>       , _:seq<'a>       ) = fun n -> Seq.skip n x

    let inline internal skip (n:int) x = Inline.instance (Skip, x) n


    type Take = Take with
        static member instance (Take, x:string        , _:string        ) = fun n -> x.[..n-1]
        static member instance (Take, x:StringBuilder , _:StringBuilder ) = fun n -> new StringBuilder(x.ToString().[..n-1])
        static member instance (Take, x:'a []         , _:'a []         ) = fun n -> x.[n..] : 'a []
        static member instance (Take, x:'a ResizeArray, _:'a ResizeArray) = fun n -> ResizeArray<'a> (Seq.take n x)
        static member instance (Take, x:list<'a>      , _:list<'a>      ) = fun n -> Seq.take n x |> Seq.toList
        static member instance (Take, x:seq<'a>       , _:seq<'a>       ) = fun n -> Seq.take n x

    let inline internal take (n:int) x = Inline.instance (Take, x) n


    type FromList = FromList with


#if NOTNET35
        static member instance (FromList, _:string        ) = fun (x:list<char>) -> String.Join("",  x |> Array.ofList)
        static member instance (FromList, _:StringBuilder ) = fun (x:list<char>) -> new StringBuilder(String.Join("", x |> Array.ofList))
#else
        static member instance (FromList, _:string        ) = fun (x:list<char>) -> String.Join("",  x |> Array.ofList |> Array.map string)
        static member instance (FromList, _:StringBuilder ) = fun (x:list<char>) -> new StringBuilder(String.Join("", x |> Array.ofList |> Array.map string))
#endif
        static member instance (FromList, _:'a []         ) = Array.ofList<'a>
        static member instance (FromList, _:'a ResizeArray) = fun (x:list<'a>)   -> ResizeArray x
        static member instance (FromList, _:list<'a>      ) = id<list<'a>>
        static member instance (FromList, _:Set<'a>       ) = Set.ofList<'a>
        static member instance (FromList, _:seq<'a>       ) = Seq.ofList<'a>

    let inline internal fromList (value:list<'t>)  = Inline.instance FromList value

    type FromSeq() =
        inherit Typ1()

#if NOTNET35
        static member instance (_:FromSeq, _:string        ) = fun (x:seq<char>) -> String.Join("", Array.ofSeq x)
        static member instance (_:FromSeq, _:StringBuilder ) = fun (x:seq<char>) -> new StringBuilder(String.Join("", Array.ofSeq x))
#else
        static member instance (_:FromSeq, _:string        ) = fun (x:seq<char>) -> String.Join("",  x |> Array.ofSeq |> Array.map string)
        static member instance (_:FromSeq, _:StringBuilder ) = fun (x:seq<char>) -> new StringBuilder(String.Join("", x |> Array.ofSeq |> Array.map string))
#endif
        static member instance (_:FromSeq, _:'a []         ) = Array.ofSeq<'a>
        static member instance (_:FromSeq, _:'a ResizeArray) = fun (x:seq<'a>)   -> ResizeArray x
        static member instance (_:FromSeq, _:list<'a>      ) = List.ofSeq<'a>
        static member instance (_:FromSeq, _:Set<'a>       ) = Set.ofSeq<'a>
        static member instance (_:FromSeq, _:seq<'a>       ) = id<seq<'a>>
        static member instance (_:FromSeq, _:'a Id         ) = fun (x:seq<'a>)   -> Id.create (Seq.head x)

    let FromSeq = FromSeq()
    let inline internal fromSeq (value:seq<'t>)  = Inline.instance FromSeq value



    type ToSeq = ToSeq with
        static member instance (_:ToSeq, x:#seq<'T>  , _:seq<'T>) = fun () -> x :> seq<_>
        static member instance (_:ToSeq, x:Id<'T>    , _:seq<'T>) = fun () -> Seq.singleton x.getValue
        static member instance (_:ToSeq, x:option<'T>, _:seq<'T>) = fun () -> match x with Some x -> Seq.singleton x | None -> Seq.empty


    type Choose = Choose with
        static member instance (_:Choose, x:Id<'T>  , _:Id<'U>  ) = fun (f:_->'U option) -> invalidOp "Choose on ID" :Id<'U> 
        static member instance (_:Choose, x:seq<'T> , _:seq<'U> ) = fun (f:_->'U option) -> Seq.choose   f x
        static member instance (_:Choose, x:list<'T>, _:list<'U>) = fun (f:_->'U option) -> List.choose  f x
        static member instance (_:Choose, x:'T []   , _:'U []   ) = fun (f:_->'U option) -> Array.choose f x

    type Distinct = Distinct with
        static member instance (_:Distinct, x:Id<'T>   , _:Id<'T>  ) = fun () -> x
        static member instance (_:Distinct, x:seq<'T>  , _:seq<'T> ) = fun () -> Seq.distinct x
        static member instance (_:Distinct, x:list<'T> , _:list<'T>) = fun () -> Seq.distinct x |> Seq.toList
        static member instance (_:Distinct, x:'T []    , _:'T []   ) = fun () -> Seq.distinct x |> Seq.toArray

    type DistinctBy = DistinctBy with
        static member instance (_:DistinctBy, x:Id<'T>   , _:Id<'T>  ) = fun f -> x
        static member instance (_:DistinctBy, x:seq<'T>  , _:seq<'T> ) = fun f -> Seq.distinctBy f x
        static member instance (_:DistinctBy, x:list<'T> , _:list<'T>) = fun f -> Seq.distinctBy f x |> Seq.toList
        static member instance (_:DistinctBy, x:'T []    , _:'T []   ) = fun f -> Seq.distinctBy f x |> Seq.toArray 

    type GroupBy = GroupBy with
        static member instance (GroupBy, x:Id<'a>  , _) = fun f -> let a = Id.run x in Id (f a, x)
        static member instance (GroupBy, x:seq<'a> , _) = fun f -> Seq.groupBy f x
        static member instance (GroupBy, x:list<'a>, _) = fun f -> Seq.groupBy f x |> Seq.map (fun (x,y) -> x, Seq.toList  y) |> Seq.toList
        static member instance (GroupBy, x:'a []   , _) = fun f -> Seq.groupBy f x |> Seq.map (fun (x,y) -> x, Seq.toArray y) |> Seq.toArray

    type GroupAdjBy = GroupAdjBy with
        static member instance (GroupAdjBy, x:Id<'a>  , _) = fun f -> let a = Id.run x in Id (f a, x)
        static member instance (GroupAdjBy, x:seq<'a> , _) = fun f -> Seq.groupAdjBy f x |> Seq.map (fun (x,y) -> x, y :> _ seq)
        static member instance (GroupAdjBy, x:list<'a>, _) = fun f -> Seq.groupAdjBy f x |> Seq.map (fun (x,y) -> x, Seq.toList  y) |> Seq.toList
        static member instance (GroupAdjBy, x:'a []   , _) = fun f -> Seq.groupAdjBy f x |> Seq.map (fun (x,y) -> x, Seq.toArray y) |> Seq.toArray


    // http://codebetter.com/matthewpodwysocki/2009/05/06/functionally-implementing-intersperse/
    let inline internal intersperse sep list = seq {
        let notFirst = ref false
        for element in list do 
            if !notFirst then yield sep
            yield element
            notFirst := true}

    type Intersperse() =
        static member instance (_:Intersperse, x:Id<'T>  , _:Id<'T>  ) = fun (e:'T) -> x
        static member instance (_:Intersperse, x:seq<'T> , _:seq<'T> ) = fun (e:'T) -> intersperse e x
        static member instance (_:Intersperse, x:list<'T>, _:list<'T>) = fun (e:'T) -> x |> List.toSeq  |> intersperse e |> Seq.toList
        static member instance (_:Intersperse, x:'T []   , _:'T []   ) = fun (e:'T) -> x |> Array.toSeq |> intersperse e |> Seq.toArray
    
    let Intersperse = Intersperse()
    

    type Iteri = Iteri with
        static member instance (_:Iteri, x:Id<'T>   , _:unit) = fun (f:int->'T->unit) -> f 0 x.getValue
        static member instance (_:Iteri, x:seq<'T>  , _:unit) = fun f -> Seq.iteri   f x
        static member instance (_:Iteri, x:list<'T> , _:unit) = fun f -> List.iteri  f x
        static member instance (_:Iteri, x:'T []    , _:unit) = fun f -> Array.iteri f x

    type Length = Length with
        static member instance (_:Length, x:Id<'T>   , _:int) = fun () -> 1
        static member instance (_:Length, x:seq<'T>  , _:int) = fun () -> Seq.length   x
        static member instance (_:Length, x:list<'T> , _:int) = fun () -> List.length  x
        static member instance (_:Length, x:'T []    , _:int) = fun () -> Array.length x

    type Mapi = Mapi with
        static member instance (_:Mapi, x:Id<'T>   , _:Id<'U>  ) = fun (f:int->'T->'U) -> f 0 x.getValue
        static member instance (_:Mapi, x:seq<'T>  , _:seq<'U> ) = fun f -> Seq.mapi   f x
        static member instance (_:Mapi, x:list<'T> , _:list<'U>) = fun f -> List.mapi  f x
        static member instance (_:Mapi, x:'T []    , _:'U []   ) = fun f -> Array.mapi f x

    type Max = Max with
        static member instance (_:Max, x:Id<'T>  , _:'T) = fun () -> x.getValue
        static member instance (_:Max, x:seq<'T> , _:'T) = fun () -> Seq.max   x
        static member instance (_:Max, x:list<'T>, _:'T) = fun () -> List.max  x
        static member instance (_:Max, x:'T []   , _:'T) = fun () -> Array.max x

    type MaxBy = MaxBy with
        static member instance (_:MaxBy, x:Id<'T>  , _:'T) = fun (f:'T->'U) -> x.getValue
        static member instance (_:MaxBy, x:seq<'T> , _:'T) = fun f -> Seq.maxBy   f x
        static member instance (_:MaxBy, x:list<'T>, _:'T) = fun f -> List.maxBy  f x
        static member instance (_:MaxBy, x:'T []   , _:'T) = fun f -> Array.maxBy f x

    type Min = Min with
        static member instance (_:Min, x:Id<'T>  , _:'T) = fun () -> x.getValue
        static member instance (_:Min, x:seq<'T> , _:'T) = fun () -> Seq.min   x
        static member instance (_:Min, x:list<'T>, _:'T) = fun () -> List.min  x
        static member instance (_:Min, x:'T []   , _:'T) = fun () -> Array.min x

    type MinBy = MinBy with
        static member instance (_:MinBy, x:Id<'T>  , _:'T) = fun f -> x.getValue
        static member instance (_:MinBy, x:seq<'T> , _:'T) = fun f -> Seq.minBy   f x
        static member instance (_:MinBy, x:list<'T>, _:'T) = fun f -> List.minBy  f x
        static member instance (_:MinBy, x:'T []   , _:'T) = fun f -> Array.minBy f x

    type Rev = Rev with
        static member instance (_:Rev, x:Id<'a>  , _:Id<'a>  ) = fun () -> x
        static member instance (_:Rev, x:seq<'a> , _:seq<'a> ) = fun () -> x |> Seq.toArray |> Array.rev |> Array.toSeq
        static member instance (_:Rev, x:list<'a>, _:list<'a>) = fun () -> List.rev  x
        static member instance (_:Rev, x:'a []   , _:'a []   ) = fun () -> Array.rev x

    type Scan = Scan with
        static member instance (_:Scan, x:Id<'T>  , _:Id<'S>  ) = fun f (z:'S) -> Id.create (f z x.getValue)
        static member instance (_:Scan, x:seq<'T> , _:seq<'S> ) = fun f (z:'S) -> Seq.scan   f z x
        static member instance (_:Scan, x:list<'T>, _:list<'S>) = fun f (z:'S) -> List.scan  f z x
        static member instance (_:Scan, x:'T []   , _:'S []   ) = fun f (z:'S) -> Array.scan f z x

    type Sort = Sort with
        static member instance (_:Sort, x:Id<'a>  , _:Id<'a>  ) = fun () -> x
        static member instance (_:Sort, x:seq<'a> , _:seq<'a> ) = fun () -> Seq.sort   x
        static member instance (_:Sort, x:list<'a>, _:list<'a>) = fun () -> List.sort  x
        static member instance (_:Sort, x:'a []   , _:'a []   ) = fun () -> Array.sort x

    type SortBy = SortBy with
        static member instance (SortBy, x:Id<'a>  , _) = fun (f:'a->_) -> x
        static member instance (SortBy, x:seq<'a> , _) = fun f -> Seq.sortBy   f x
        static member instance (SortBy, x:list<'a>, _) = fun f -> List.sortBy  f x
        static member instance (SortBy, x:'a []   , _) = fun f -> Array.sortBy f x
        
    type Zip = Zip with
        static member instance (_:Zip, x:Id<'T>  , y:Id<'U>  , _:Id<'T*'U>  ) = fun () -> Id.create(x.getValue,y.getValue)
        static member instance (_:Zip, x:seq<'T> , y:seq<'U> , _:seq<'T*'U> ) = fun () -> Seq.zip   x y
        static member instance (_:Zip, x:list<'T>, y:list<'U>, _:list<'T*'U>) = fun () -> List.zip  x y
        static member instance (_:Zip, x:'T []   , y:'U []   , _:('T*'U) [] ) = fun () -> Array.zip x y