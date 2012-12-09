﻿namespace FsControl.Core

module internal Prelude =
    let inline flip f x y = f y x
    let inline const' k _ = k
    let inline (</) x = (|>) x
    let inline (/>) x = flip x
    let inline (++) x = (@)  x
    let inline (==) x = (=)  x
    let inline (=/) x y = not (x = y)
    let inline choice f g = function Choice2Of2 x -> f x | Choice1Of2 y -> g y
    let inline maybe  n f = function | None -> n | Some x -> f x

    let inline  internal map x = List.map x
    let inline  internal singleton x = [x]
    let inline  internal concat (x:List<List<'a>>) :List<'a> = List.concat x
    let inline  internal cons x y = x :: y

    type DeReference = DeReference with
        static member instance (DeReference, a:'a ref     , _) = fun () -> !a
        static member instance (DeReference, a:string     , _) = fun () -> a.ToCharArray() |> Array.toList
        static member instance (DeReference, a:DeReference, _) = fun () -> DeReference

    let inline (!) a = Inline.instance (DeReference, a) ()







