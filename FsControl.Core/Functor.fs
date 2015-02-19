namespace FsControl.Core.Types

type Kleisli<'a, 'm> = Kleisli of ('a -> 'm)

[<RequireQualifiedAccess>]
module Kleisli = let run (Kleisli f) = f

namespace FsControl.Core.TypeMethods

open System
open System.Text
open System.Collections.Generic
#if NOTNET35
open System.Threading.Tasks
#endif
open Microsoft.FSharp.Quotations
open FsControl.Core
open FsControl.Core.Prelude
open FsControl.Core.Types


// Monad class ------------------------------------------------------------

type Bind() =
    inherit Default1()
    static member val Instance = Bind()

    static member inline Bind (_:Default1, x:'M, r:'R) = fun (f:'t->'R) -> 
        ((^M or ^R) : (static member Bind: ^M -> (('t->'R) -> 'R)) x) f

    static member        Bind (_:Bind, x:Lazy<'a>     , _:Lazy<'b>     ) = fun (f:_->Lazy<'b>  ) -> lazy (f x.Value).Value
    static member        Bind (_:Bind, x:seq<_>       , _:seq<'b>      ) = fun (f:_->seq<'b>   ) -> Seq.bind f x
    static member        Bind (_:Bind, x:Id<'a>       , _:'b Id        ) = fun (f:_->Id<'b>    ) -> f x.getValue

#if NOTNET35
    static member        Bind (_:Bind, x:Task<'a>     , _:'b Task      ) = fun (f:_->Task<'b>  ) -> x.ContinueWith(fun (x: Task<_>) -> f x.Result).Unwrap()
#endif

    static member        Bind (_:Bind, x:option<_>    , _:option<'b>   ) = fun (f:_->option<'b>) -> Option.bind   f x
    static member        Bind (_:Bind, x:list<_>      , _:list<'b>     ) = fun (f:_->list<'b>  ) -> List.collect  f x
    static member        Bind (_:Bind, x:_ []         , _:'b []        ) = fun (f:_->'b []     ) -> Array.collect f x
    static member        Bind (_:Bind, f:'r->'a       , _:'r->'b       ) = fun (k:_->_->'b) r    -> k (f r) r
    static member inline Bind (_:Bind, (w, a):'m * 'a , _:'m * 'b      ) = fun (k:_->'m * 'b   ) -> let m, b = k a in (Mappend.Invoke w m, b)
    static member        Bind (_:Bind, x:Async<'a>    , _:'b Async     ) = fun (f:_->Async<'b> ) -> async.Bind(x,f)
    static member        Bind (_:Bind, x:Choice<'a,'e>, _:Choice<'b,'e>) = fun (k:'a->Choice<'b,'e>) -> Error.bind k x

    static member        Bind (_:Bind, x:Map<'k,'a>   , _:Map<'k,'b>   ) = fun (f:'a->Map<'k,'b>) -> Map (seq {
        for KeyValue(k, v) in x do
            match Map.tryFind k (f v) with
            | Some v -> yield k, v
            | _      -> () })

    static member        Bind (_:Bind, x:Dictionary<'k,'a>   , _:Dictionary<'k,'b>   ) = fun (f:'a->Dictionary<'k,'b>) -> 
        let d = Dictionary()
        for KeyValue(k, v) in x do
            match (f v).TryGetValue(k)  with
            | true, v -> d.Add(k, v)
            | _       -> ()
        d

    //Restricted Monad
    static member Bind (_:Bind, x:Nullable<_> , _:'b Nullable) = fun f -> if x.HasValue then f x.Value else Nullable() : Nullable<'b>

    static member inline Invoke x (f:_->'R) : 'R =
        let inline call_3 (a:^a,b:^b,c:^c) = ((^a or ^b or ^c) : (static member Bind: ^a* ^b* ^c -> _) (a,b,c))
        call_3 (Bind.Instance, x, Unchecked.defaultof<'R>) f :'R


type Join() =
    inherit Default1()
    static member val Instance = Join()

    static member inline Join (_:Default1, x               , _           ) = Bind.Invoke x id 
    static member        Join (_:Join, x:Lazy<Lazy<'a>>    , _:Lazy<'a>  ) = lazy x.Value.Value
    static member        Join (_:Join, x:option<option<'a>>, _:option<'a>) = Option.bind   id x
    static member        Join (_:Join, x:list<_>           , _:list<'b>  ) = List.collect  id x
    static member        Join (_:Join, x:'b [] []          , _:'b []     ) = Array.collect id x
    static member        Join (_:Join, x:Id<Id<'a>>        , _:Id<'a>    ) = x.getValue

#if NOTNET35        
    static member        Join (_:Join, x:Task<Task<'a>>    , _:Task<'a>  ) = x.Unwrap()
#endif

    static member inline Invoke (x:'Monad'Monad'a) : 'Monad'a =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member Join: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = call_3 (a, b, Unchecked.defaultof<'r>):'r
        call (Join.Instance, x)


type Return() =
    inherit Default1()
    static member val Instance = Return()
    static member inline Return (_:Default1, r:'R) = fun (x:'T) -> ((^R) : (static member Return: ^T -> ^R) x)

    static member        Return (_:Return, _:Lazy<'a>      ) = fun x -> Lazy.CreateFromValue x : Lazy<'a>
    static member        Return (_:Return, _:seq<'a>       ) = fun x -> Seq.singleton x :seq<'a>
    static member        Return (_:Return, _:Id<'a>        ) = fun x -> Id x :Id<'a>

#if NOTNET35        
    static member        Return (_:Return, _:'a Task       ) = fun x -> 
        let s = TaskCompletionSource()
        s.SetResult x
        s.Task
#endif        
    static member        Return (_:Return, _:option<'a>    ) = fun x -> Some x      :option<'a>
    static member        Return (_:Return, _:list<'a>      ) = fun x -> [ x ]       :list<'a>
    static member        Return (_:Return, _:'a []         ) = fun x -> [|x|]       :'a []
    static member        Return (_:Return, _:'r -> 'a      ) = const':'a  -> 'r -> _
    static member inline Return (_:Return, _: 'm * 'a      ) = fun (x:'a) -> (Mempty.Invoke(), x)
    static member        Return (_:Return, _:'a Async      ) = fun (x:'a) -> async.Return x
    static member        Return (_:Return, _:Choice<'a,'e> ) = fun x -> Choice1Of2 x :Choice<'a,'e>
    static member        Return (_:Return, _:Expr<'a>      ) = fun x -> Expr.Cast<'a>(Expr.Value(x))
    static member        Return (_:Return, _:'a ResizeArray) = fun x -> ResizeArray<'a>(Seq.singleton x)

    //Restricted
    static member Return (_:Return, _:'a Nullable  ) = fun (x:'a  ) -> Nullable x:'a Nullable
    static member Return (_:Return, _:string       ) = fun (x:char) -> string x : string
    static member Return (_:Return, _:StringBuilder) = fun (x:char) -> new StringBuilder(string x):StringBuilder
    static member Return (_:Return, _:'a Set       ) = fun (x:'a  ) -> Set.singleton x

    static member inline Invoke x = 
        let inline call_2 (a:^a,b:^b) = ((^a or ^b) : (static member Return: ^a* ^b -> _) (a,b))
        call_2 (Return.Instance, Unchecked.defaultof<'r>) x :'r 
 

type Apply() =
    inherit Default1()
    static member val Instance = Apply()
    static member inline FromMonad f x = Bind.Invoke f (fun x1 -> Bind.Invoke x (fun x2 -> Return.Invoke(x1 x2)))


    static member inline Apply (_:Default2, f , x, _) = Bind.Invoke f (fun x1 -> Bind.Invoke x (fun x2 -> Return.Invoke(x1 x2)))
    static member inline Apply (_:Default1, f:'F , x:'X, _:'R) = ((^F or ^X or ^R) : (static member (<*>): ^F -> ^X -> 'R) (f, x))

    static member        Apply (_:Apply, f:Lazy<'a->'b>, x:Lazy<'a>     , _:Lazy<'b>     ) = Lazy.Create (fun () -> f.Value x.Value) : Lazy<'b>
    static member        Apply (_:Apply, f:seq<_>      , x:seq<'a>      , _:seq<'b>      ) = Seq.apply  f x :seq<'b>
    static member        Apply (_:Apply, f:list<_>     , x:list<'a>     , _:list<'b>     ) = List.apply f x :list<'b>
    static member        Apply (_:Apply, f:_ []        , x:'a []        , _:'b []        ) = Apply.FromMonad f x :'b []
    static member        Apply (_:Apply, f:'r -> _     , g: _ -> 'a     , _: 'r -> 'b    ) = fun x -> f x (g x) :'b
    static member inline Apply (_:Apply, (a:'m, f)     , (b:'m, x:'a)   , _:'m * 'b      ) = (Mappend.Invoke a b, f x) :'m *'b
    static member        Apply (_:Apply, f:Async<_>    , x:Async<'a>    , _:Async<'b>    ) = Apply.FromMonad f x :Async<'b>
    static member        Apply (_:Apply, f:option<_>   , x:option<'a>   , _:option<'b>   ) = Option.apply f x
    static member        Apply (_:Apply, f:Choice<_,'e>, x:Choice<'a,'e>, _:Choice<'b,'e>) = Error.apply f x :Choice<'b,'e>

    static member        Apply (_:Apply, KeyValue(k:'k,f)  , KeyValue(k:'k,x:'a), _:KeyValuePair<'k,'b>) :KeyValuePair<'k,'b> = KeyValuePair(k, f x)

    static member        Apply (_:Apply, f:Map<'k,_>       , x:Map<'k,'a>       , _:Map<'k,'b>         ) :Map<'k,'b>          = Map (seq {
        for KeyValue(k, vf) in f do
            match Map.tryFind k x with
            | Some vx -> yield k, vf vx
            | _       -> () })

    static member        Apply (_:Apply, f:Dictionary<'k,_>, x:Dictionary<'k,'a>, _:Dictionary<'k,'b>  ) :Dictionary<'k,'b> =
        let d = Dictionary()
        for KeyValue(k, vf) in f do
            match x.TryGetValue k with
            | true, vx -> d.Add(k, vf vx)
            | _        -> ()
        d

    static member        Apply (_:Apply, f:Expr<'a->'b>, x:Expr<'a>, _:Expr<'b>) = Expr.Cast<'b>(Expr.Application(f,x))

    static member        Apply (_:Apply, f:('a->'b) ResizeArray, x:'a ResizeArray, _:'b ResizeArray) =
        ResizeArray(Seq.collect (fun x1 -> Seq.collect (fun x2 -> Seq.singleton (x1 x2)) x) f) :'b ResizeArray

    static member  inline internal Invoke x y =
        let inline call_4 (a:^a,b:^b,c:^c,d:^d) =                                                          
            ((^a or ^b or ^c or ^d) : (static member Apply: ^a* ^b* ^c* ^d -> _) (a,b,c,d))
        let inline call (a:'a, b:'b, c:'c) = call_4(a,b,c, Unchecked.defaultof<'r>) : 'r
        call (Apply.Instance, x, y) : 'Applicative'U


// Functor class ----------------------------------------------------------

type Map_() =
    static member val Instance = Map_()
    static member Map_ (_:Map_, x:Lazy<_>        , _:unit) = fun f -> f x.Value :unit
    static member Map_ (_:Map_, x:seq<_>         , _:unit) = fun f -> Seq.iter f x
    static member Map_ (_:Map_, x:option<_>      , _:unit) = fun f -> match x with Some x -> f x | _ -> ()
    static member Map_ (_:Map_, x:list<_>        , _:unit) = fun f -> List.iter f x
    static member Map_ (_:Map_, (m,a)            , _:unit) = fun f -> f a :unit
    static member Map_ (_:Map_, x:_ []           , _:unit) = fun f -> Array.iter   f x
    static member Map_ (_:Map_, x:_ [,]          , _:unit) = fun f -> Array2D.iter f x
    static member Map_ (_:Map_, x:_ [,,]         , _:unit) = fun f -> Array3D.iter f x
    static member Map_ (_:Map_, x:_ [,,,]        , _:unit) = fun f ->
        for i = 0 to Array4D.length1 x - 1 do
            for j = 0 to Array4D.length2 x - 1 do
                for k = 0 to Array4D.length3 x - 1 do
                    for l = 0 to Array4D.length4 x - 1 do
                        f x.[i,j,k,l]
    static member Map_ (_:Map_, x:Async<_>       , _:unit) = fun f -> f (Async.RunSynchronously x) : unit
    static member Map_ (_:Map_, x:Choice<_,_>    , _:unit) = fun f -> match x with Choice1Of2 x -> f x | _ -> ()
    static member Map_ (_:Map_, KeyValue(k, x)   , _:unit) = fun f -> f x :unit
    static member Map_ (_:Map_, x:Map<'a,'b>     , _:unit) = fun f -> Map.iter (const' f) x 
    static member Map_ (_:Map_, x:Dictionary<_,_>, _:unit) = fun f -> Seq.iter f x.Values
    static member Map_ (_:Map_, x:_ ResizeArray  , _:unit) = fun f -> Seq.iter f x

    // Restricted
    static member Map_ (_:Map_, x:Nullable<_>    , _:unit) = fun f -> if x.HasValue then f x.Value else ()
    static member Map_ (_:Map_, x:string         , _:unit) = fun f -> String.iter f x
    static member Map_ (_:Map_, x:StringBuilder  , _:unit) = fun f -> String.iter f (x.ToString())
    static member Map_ (_:Map_, x:Set<_>         , _:unit) = fun f -> Set.iter f x        

    static member inline Invoke (action :'T->unit) (source :'Functor'T) =
        let inline call_3 (a:^a,b:^b,c:^c) =  ((^a or ^b or ^c) : (static member Map: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = fun (x:'x) -> call_3 (a ,b , Unchecked.defaultof<'r>) x :'r
        call (Map_.Instance, source) action :unit

type Map() =
    inherit Default1()
    static member val Instance = Map()
         
    static member inline FromApplicative f x = Return.Invoke (Apply.Invoke f x)
    static member inline FromMonad       f x = Bind.Invoke x (Return.Invoke << f)

    static member inline Map (_:Default2, x:'f, _:'r) = fun (f:'a->'b) -> Return.Invoke (Apply.Invoke f x) :'r
    static member inline Map (_:Default1, x:'F, _:'R) = fun (f:'a->'b) -> ((^F) : (static member (<!>): ('a->'b) -> ^F -> ^R) (f, x))

    static member Map (_:Map, x:Lazy<_>        , _:Lazy<'b>) = fun f -> Lazy.Create (fun () -> f x.Value) : Lazy<'b>
    static member Map (_:Map, x:seq<_>         , _:seq<'b> ) = fun f -> Seq.map f x :seq<'b>
    static member Map (_:Map, x:option<_>      , _) = fun f -> Option.map  f x
    static member Map (_:Map, x:list<_>        , _:list<'b>) = fun f -> List.map f x :list<'b>
    static member Map (_:Map, g:_->_           , _) = (>>) g
    static member Map (_:Map, (m,a)            , _) = fun f -> (m, f a)
    static member Map (_:Map, x:_ []           , _) = fun f -> Array.map   f x
    static member Map (_:Map, x:_ [,]          , _) = fun f -> Array2D.map f x
    static member Map (_:Map, x:_ [,,]         , _) = fun f -> Array3D.map f x
    static member Map (_:Map, x:_ [,,,]        , _) = fun f -> Array4D.init (x.GetLength 0) (x.GetLength 1) (x.GetLength 2) (x.GetLength 3) (fun a b c d -> f x.[a,b,c,d])
    static member Map (_:Map, x:Async<_>       , _) = fun f -> Map.FromMonad f x
    static member Map (_:Map, x:Choice<_,_>    , _) = fun f -> Error.map f x
    static member Map (_:Map, KeyValue(k, x)   , _) = fun (f:'b->'c) -> KeyValuePair(k, f x)
    static member Map (_:Map, x:Map<'a,'b>     , _) = fun (f:'b->'c) -> Map.map (const' f) x : Map<'a,'c>
    static member Map (_:Map, x:Dictionary<_,_>, _) = fun (f:'b->'c) -> let d = Dictionary() in Seq.iter (fun (KeyValue(k, v)) -> d.Add(k, f v)) x; d: Dictionary<'a,'c>
    static member Map (_:Map, x:Expr<'a>       , _) = fun (f:'a->'b) -> Expr.Cast<'b>(Expr.Application(Expr.Value(f),x))
    static member Map (_:Map, x:_ ResizeArray  , _) = fun f -> ResizeArray(Seq.map f x) : ResizeArray<'b>
    static member Map (_:Map, x:_ IObservable  , _) = fun f -> Observable.map f x

    // Restricted
    static member Map (_:Map, x:Nullable<_>    , _) = fun f -> if x.HasValue then Nullable(f x.Value) else Nullable()
    static member Map (_:Map, x:string         , _) = fun f -> String.map f x
    static member Map (_:Map, x:StringBuilder  , _) = fun f -> new StringBuilder(String.map f (x.ToString()))
    static member Map (_:Map, x:Set<_>         , _) = fun f -> Set.map f x
        
    static member inline Invoke f x = 
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member Map: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = fun (x:'x) -> call_3 (a, b, Unchecked.defaultof<'r>) x :'r
        call (Map.Instance, x) f


type Zero() =
    static member val Instance = Zero()
    static member        Zero (_:Zero, _:option<'a>) = None        :option<'a>
    static member        Zero (_:Zero, _:list<'a>  ) = [  ]        :list<'a>  
    static member        Zero (_:Zero, _:'a []     ) = [||]        :'a []     
    static member        Zero (_:Zero, _:seq<'a>   ) = Seq.empty   :seq<'a>
    static member inline Zero (_:Zero, _:Id<'a>    ) = Id (Mempty.Invoke()) :Id<'a>

    static member inline Invoke () :'Functor'T =
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member Zero: _*_ -> _) a, b)
        let inline call (a:'a) = call_2 (a, Unchecked.defaultof<'r>) :'r
        call Zero.Instance


type Plus() =
    static member val Instance = Plus()
    static member        Plus (_:Plus, x:_ option, _) = fun y -> match x with None -> y | xs -> xs
    static member        Plus (_:Plus, x:_ list  , _) = fun y -> x @ y
    static member        Plus (_:Plus, x:_ []    , _) = fun y -> Array.append x y
    static member        Plus (_:Plus, x:_ seq   , _) = fun y -> Seq.append   x y
    static member inline Plus (_:Plus, x:_ Id    , _) = fun y -> Id (Mappend.Invoke (Id.run x) (Id.run y))

    static member inline Invoke (x:'Functor'T) (y:'Functor'T) :'Functor'T =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member Plus: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = fun (x:'x) -> call_3 (a, b, Unchecked.defaultof<'r>) x :'r        
        call (Plus.Instance, x) y

type Zero with
    static member inline Zero (_:Zero, _:Kleisli<_,_>) = Kleisli (fun _ -> Zero.Invoke ())
    
type Plus with
    static member inline Plus (_:Plus, Kleisli f, _) = fun (Kleisli g) -> Kleisli(fun x -> Plus.Invoke (f x) (g x))


module Monad =

    let inline (>>=) f x = Bind.Invoke f x
    let inline result  x = Return.Invoke x
    let inline (<*>) f x = Apply.Invoke f x
    let inline (<|>) x y = Plus.Invoke x y

    let inline internal sequence ms =
        let k m m' = m >>= fun (x:'a) -> m' >>= fun xs -> (result :list<'a> -> 'M) (List.Cons(x,xs))
        List.foldBack k ms ((result :list<'a> -> 'M) [])

    let inline internal mapM f as' = sequence (List.map f as')

    let inline internal liftM  f m1    = m1 >>= (result << f)
    let inline internal liftM2 f m1 m2 = m1 >>= fun x1 -> m2 >>= fun x2 -> result (f x1 x2)
    let inline internal ap     x y     = liftM2 id x y

    let inline internal (>=>)  f g x   = f x >>= g

    // Do notation ------------------------------------------------------------

    type DoNotationBuilder() =
        member inline b.Return(x)    = result x
        member inline b.Bind(p,rest) = p >>= rest
        member        b.Let (p,rest) = rest p
        member    b.ReturnFrom(expr) = expr
    let inline  internal do'() = new DoNotationBuilder()


open Monad

type Extract() =
    static member val Instance = Extract()
    static member        Extract (_:Extract, x:'t Async,_:'t) = Async.RunSynchronously x
    static member        Extract (_:Extract, x:'t Lazy, _:'t) = x.Value
    static member        Extract (_:Extract, (w:'w,a:'a) , _) = a
    static member inline Extract (_:Extract, f:'m->'t , _:'t) = f (Mempty.Invoke())
    static member        Extract (_:Extract, f:'t Id  , _:'t) = f

#if NOTNET35
    static member        Extract (_:Extract, f:'t Task,_:'t) = f.Result
#endif

    // Restricted        
    static member        Extract (_:Extract, x:'t list         , _:'t) = List.head x
    static member        Extract (_:Extract, x:'t []           , _:'t) = x.[0]
    static member        Extract (_:Extract, x:string          , _   ) = x.[0]
    static member        Extract (_:Extract, x:StringBuilder   , _   ) = x.ToString().[0]
    static member        Extract (_:Extract, x:'t seq          , _:'t) = Seq.head x

    static member inline Invoke (x:'Comonad'T): 'T =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member Extract: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = call_3 (a, b, Unchecked.defaultof<'r>) :'r
        call (Extract.Instance, x)

type Extend() =
    static member val Instance = Extend()
    static member        Extend (_:Extend, (g:'a Async), _:'b Async) = fun (f:Async<'a>->'b) -> async.Return (f g) : Async<'b>
    static member        Extend (_:Extend, (g:'a Lazy ), _:'b Lazy ) = fun (f:Lazy<'a> ->'b) -> Lazy.Create  (fun () -> f g) : Lazy<'b>
    static member        Extend (_:Extend, (w:'w, a:'a), _:'w *'b)   = fun (f:_->'b) -> (w, f (w,a))        
    static member inline Extend (_:Extend, (g:'m -> 'a), _:'m->'b)   = fun (f:_->'b) a -> f (fun b -> g (Mappend.Invoke a b))
    static member        Extend (_:Extend, (g:'a Id   ), _:'b Id )   = fun (f:Id<'a>->'b) -> f g

#if NOTNET35
    static member        Extend (_:Extend, (g:'a Task), _:'b Task) = fun (f:Task<'a>->'b) -> g.ContinueWith(f)
#endif

    // Restricted
    static member        Extend (_:Extend, s:list<'a>, _:list<'b>) = fun g -> 
        let rec tails = function [] -> [] | x::xs as s -> s::(tails xs)
        List.map g (tails s) :list<'b>

    static member        Extend (_:Extend, s:'a [], _:'b []) = fun g -> 
        let rec tails = function [] -> [] | x::xs as s -> s::(tails xs)
        Array.map g (s |> Array.toList |> tails |> List.toArray |> Array.map List.toArray) :'b []

    static member        Extend (_:Extend, s:'a seq, _:'b seq) = fun g -> 
        let rec tails = function [] -> [] | x::xs as s -> s::(tails xs)
        Seq.map g (s |> Seq.toList |> tails |> List.toSeq |> Seq.map List.toSeq) :'b seq

    static member inline Invoke (g:'Comonad'T->'U) (s:'Comonad'T): 'Comonad'U =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member Extend: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = fun (x:'x) -> call_3 (a, b, Unchecked.defaultof<'r>) x :'r
        call (Extend.Instance, s) g


type Duplicate() =
    inherit Default1()
    static member val Instance = Duplicate()
    static member inline Duplicate (_:Default1 , x           , _                 ) = Extend.Invoke id x
    static member        Duplicate (_:Duplicate, s:Async<'a> , _:Async<Async<'a>>) = async.Return s : Async<Async<'a>>
    static member        Duplicate (_:Duplicate, s:Lazy<'a>  , _:Lazy<Lazy<'a>>  ) = Lazy.CreateFromValue s : Lazy<Lazy<'a>>
    static member        Duplicate (_:Duplicate, (w:'w, a:'a), _:'w * ('w*'a)    ) = (w, (w, a))
    static member inline Duplicate (_:Duplicate,  f:'m -> 'a , _:'m->'m->'a      ) = fun a b -> f (Mappend.Invoke a b)

    // Restricted
    static member        Duplicate (_:Duplicate, s:list<'a>, _:list<list<'a>>) =
        let rec tails = function [] -> [] | x::xs as s -> s::(tails xs)
        tails s

    static member        Duplicate (_:Duplicate, s: array<'a>, _: array<array<'a>>) =
        let rec tails = function [] -> [] | x::xs as s -> s::(tails xs)
        s |> Array.toList |> tails |> List.toArray |> Array.map List.toArray
    
    static member inline Invoke x =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member Duplicate: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = call_3 (a, b, Unchecked.defaultof<'r>) :'r
        call (Duplicate.Instance, x)




type Id() =
    static member val Instance = Id()
    static member        Id (_:Id, _: 'r -> 'r     ) = id              : 'r -> 'r
    static member inline Id (_:Id, _:Kleisli<'a,'b>) = Kleisli result :Kleisli<'a,'b>

    static member inline Invoke() =
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member Id: _*_ -> _) a, b)
        let inline call (a:'a) = call_2 (a, Unchecked.defaultof<'r>) :'r
        call Id.Instance


type Comp() =
    static member val Instance = Comp()
    static member        Comp (_:Comp,         f, _) = fun (g: _ -> _) ->          g >>  f
    static member inline Comp (_:Comp, Kleisli f, _) = fun (Kleisli g) -> Kleisli (g >=> f)

    static member inline Invoke f g =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member Comp: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = fun (x:'x) -> call_3 (a, b, Unchecked.defaultof<'r>) x :'r
        call (Comp.Instance, f) g


type Arr() =
    static member val Instance = Arr()
    static member        Arr (_:Arr, _: _ -> _     ) = fun (f:_->_) -> f
    static member inline Arr (_:Arr, _:Kleisli<_,_>) = fun  f       -> Kleisli (Comp.Invoke result f)

    static member inline Invoke f = 
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member Arr: _*_ -> _) a, b)
        let inline call (a:'a) = fun (x:'x) -> call_2 (a, Unchecked.defaultof<'r>) x :'r
        call Arr.Instance f


type First() =
    static member val Instance = First()
    static member        First (_:First, f        , _: 'a -> 'b   ) = fun (x,y) -> (f x, y)
    static member inline First (_:First, Kleisli f, _:Kleisli<_,_>) = Kleisli (fun (b,d) -> f b >>= fun c -> result (c,d))

    static member inline Invoke f =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member First: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = call_3 (a, b, Unchecked.defaultof<'r>) :'r
        call (First.Instance, f)


type Second() =
    inherit Default1()
    static member val Instance = Second()
    static member inline Second (_:Default1, f      , _             ) = let aswap = Arr.Invoke (fun (x,y) -> (y,x)) in Comp.Invoke aswap (Comp.Invoke (First.Invoke f) aswap)
    static member        Second (_:Second, f        , _: 'a -> 'b   ) = fun (x,y) -> (x, f y)
    static member inline Second (_:Second, Kleisli f, _:Kleisli<_,_>) = Kleisli (fun (d,b) -> f b >>= fun c -> result (d,c))

    static member inline Invoke  f   =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member Second: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = call_3 (a, b, Unchecked.defaultof<'r>) :'r
        call (Second.Instance, f)

type AcEither() =
    static member val Instance = AcEither()
    static member inline AcEither (_:AcEither, _:Choice<_,_>->_) = fun (         f ,          g ) ->          choice f g
    static member inline AcEither (_:AcEither, _:Kleisli<_,_>  ) = fun ((Kleisli f), (Kleisli g)) -> Kleisli (choice f g)

    static member inline Invoke f g =
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member AcEither: _*_ -> _) a, b)
        let inline call (a:'a) = fun (x:'x) -> call_2 (a, Unchecked.defaultof<'r>) x :'r
        call AcEither.Instance (f, g)


type AcMerge() =
    static member val Instance = AcMerge()
    static member inline AcMerge (_:AcMerge, _: _->    Choice<_,_>      ) = fun (f, g)  -> AcEither.Invoke (Choice2Of2 << f) (Choice1Of2 << g)
    static member inline AcMerge (_:AcMerge, _:Kleisli<Choice<'v,'t>,'z>) = fun ((Kleisli (f:'t->'u)), (Kleisli (g:'v->'w))) ->
        AcEither.Invoke (Kleisli (f >=> (Comp.Invoke result Choice2Of2))) (Kleisli (g >=> (Comp.Invoke result Choice1Of2))) :Kleisli<Choice<'v,'t>,'z>     

    static member inline Invoke f g =
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member AcMerge: _*_ -> _) a, b)
        let inline call (a:'a) = fun (x:'x) -> call_2 (a, Unchecked.defaultof<'r>) x :'r
        call AcMerge.Instance (f, g)


type AcLeft() =
    static member val Instance = AcLeft()
    static member inline AcLeft (_:AcLeft, f:_->_   , _) = AcMerge.Invoke f id
    static member inline AcLeft (_:AcLeft, Kleisli f, _) =
        let inline (+++) a b = AcMerge.Invoke a b
        (+++) (Kleisli f) (Arr.Invoke (Id.Invoke()))

    static member inline Invoke    f   =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member AcLeft: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = call_3 (a, b, Unchecked.defaultof<'r>) :'r
        call (AcLeft.Instance, f)


type AcRight() =
    static member val Instance = AcRight()
    static member inline AcRight (_:AcRight, f:_->_   , _) = AcMerge.Invoke id f
    static member inline AcRight (_:AcRight, Kleisli f, _) =
        let inline (+++) a b = AcMerge.Invoke a b
        (+++) (Arr.Invoke (Id.Invoke())) (Kleisli f)

    static member inline Invoke   f   =
        let inline call_3 (a:^a, b:^b, c:^c) = ((^a or ^b or ^c) : (static member AcRight: _*_*_ -> _) a, b, c)
        let inline call (a:'a, b:'b) = call_3 (a, b, Unchecked.defaultof<'r>) :'r
        call (AcRight.Instance, f)


type ArrApply() =
    static member val Instance = ArrApply()
    static member ArrApply (_:ArrApply, _: ('a -> 'b) * 'a -> 'b          ) =          fun (f, x)         -> f x
    static member ArrApply (_:ArrApply, _: Kleisli<Kleisli<'a,'b> * 'a,'b>) = Kleisli (fun (Kleisli f, x) -> f x)

    static member inline Invoke()     =
        let inline call_2 (a:^a, b:^b) = ((^a or ^b) : (static member ArrApply: _*_ -> _) a, b)
        let inline call (a:'a) = call_2 (a, Unchecked.defaultof<'r>) :'r
        call ArrApply.Instance