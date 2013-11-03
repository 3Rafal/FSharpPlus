﻿namespace FsControl.Core.NumericH98.TypeMethods


open System.Numerics
open FsControl.Core.Prelude

module Num =
    type FromInteger = FromInteger with
        static member        instance (FromInteger, _:sbyte     ) = fun (x:bigint) -> sbyte           x
        static member        instance (FromInteger, _:int16     ) = fun (x:bigint) -> int16           x
        static member        instance (FromInteger, _:int32     ) = fun (x:bigint) -> int             x
        static member        instance (FromInteger, _:int64     ) = fun (x:bigint) -> int64           x
        static member        instance (FromInteger, _:nativeint ) = fun (x:bigint) -> nativeint  (int x)
        static member        instance (FromInteger, _:byte      ) = fun (x:bigint) -> byte            x
        static member        instance (FromInteger, _:uint16    ) = fun (x:bigint) -> uint16          x
        static member        instance (FromInteger, _:uint32    ) = fun (x:bigint) -> uint32          x
        static member        instance (FromInteger, _:uint64    ) = fun (x:bigint) -> uint64          x
        static member        instance (FromInteger, _:unativeint) = fun (x:bigint) -> unativeint (int x)
        static member        instance (FromInteger, _:bigint    ) = fun (x:bigint) ->                 x
        static member        instance (FromInteger, _:float     ) = fun (x:bigint) -> float           x
        static member        instance (FromInteger, _:float32   ) = fun (x:bigint) -> float32         x    
        static member        instance (FromInteger, _:decimal   ) = fun (x:bigint) -> decimal         x
        static member        instance (FromInteger, _:Complex   ) = fun (x:bigint) -> Complex (float  x, 0.0)

    type Abs = Abs with
        static member inline instance (Abs, _:^t when ^t: null and ^t: struct, _) = fun () -> id
        static member inline instance (Abs, x:'t        , _) = fun () -> abs x
        static member        instance (Abs, x:byte      , _) = fun () ->     x
        static member        instance (Abs, x:uint16    , _) = fun () ->     x
        static member        instance (Abs, x:uint32    , _) = fun () ->     x
        static member        instance (Abs, x:uint64    , _) = fun () ->     x
        static member        instance (Abs, x:unativeint, _) = fun () ->     x
        static member        instance (Abs, x:Complex   , _) = fun () -> Complex(x.Magnitude, 0.0)

    type Signum = Signum with
        static member inline instance (Signum, _:^t when ^t: null and ^t: struct, _) = fun () -> id
        static member inline instance (Signum, x:'t        , _) = fun () -> Inline.instance FromInteger (bigint (sign x)) :'t
        static member        instance (Signum, x:byte      , _) = fun () -> if x = 0uy then 0uy else 1uy
        static member        instance (Signum, x:uint16    , _) = fun () -> if x = 0us then 0us else 1us
        static member        instance (Signum, x:uint32    , _) = fun () -> if x = 0u  then 0u  else 1u
        static member        instance (Signum, x:uint64    , _) = fun () -> if x = 0UL then 0UL else 1UL
        static member        instance (Signum, x:unativeint, _) = fun () -> if x = 0un then 0un else 1un
        static member        instance (Signum, x:Complex   , _) = fun () -> 
            if x.Magnitude = 0.0 then Complex.Zero
            else Complex(x.Real / x.Magnitude, x.Imaginary / x.Magnitude)

    type Negate = Negate with
        static member inline instance (Negate, _:^t when ^t: null and ^t: struct, _) = fun () -> id
        static member inline instance (Negate, x:'t        , _) = fun () -> -x
        static member        instance (Negate, x:byte      , _) = fun () -> 0uy - x
        static member        instance (Negate, x:uint16    , _) = fun () -> 0us - x
        static member        instance (Negate, x:uint32    , _) = fun () -> 0u  - x
        static member        instance (Negate, x:uint64    , _) = fun () -> 0UL - x
        static member        instance (Negate, x:unativeint, _) = fun () -> 0un - x

    let inline internal opPlus     (a:'Num) (b:'Num) :'Num = a + b
    let inline internal opMinus    (a:'Num) (b:'Num) :'Num = a - b
    let inline internal opMultiply (a:'Num) (b:'Num) :'Num = a * b


// Integral class ---------------------------------------------------------
module Integral =
    type ToInteger = ToInteger with
        static member        instance (ToInteger, x:sbyte     , _) = fun () -> bigint (int x)
        static member        instance (ToInteger, x:int16     , _) = fun () -> bigint (int x)
        static member        instance (ToInteger, x:int32     , _) = fun () -> bigint      x
        static member        instance (ToInteger, x:int64     , _) = fun () -> bigint      x
        static member        instance (ToInteger, x:nativeint , _) = fun () -> bigint (int x)
        static member        instance (ToInteger, x:byte      , _) = fun () -> bigint (int x)
        static member        instance (ToInteger, x:uint16    , _) = fun () -> bigint (int x)
        static member        instance (ToInteger, x:uint32    , _) = fun () -> bigint      x
        static member        instance (ToInteger, x:uint64    , _) = fun () -> bigint      x
        static member        instance (ToInteger, x:unativeint, _) = fun () -> bigint (int x)
        static member        instance (ToInteger, x:bigint    , _) = fun () ->             x



open System.Numerics

module internal Numerics =
    let inline internal fromInteger (x:bigint) :'Num = Inline.instance Num.FromInteger x
    let inline internal abs (x:'Num) :'Num = Inline.instance (Num.Abs, x) ()
    let inline internal signum (x:'Num) :'Num = Inline.instance (Num.Signum, x) ()
    let inline internal negate (x:'Num) :'Num = Inline.instance (Num.Negate, x) ()

    let inline internal toInteger (x:'Integral) :bigint = Inline.instance (Integral.ToInteger, x) ()

    let inline internal fromIntegral (x:'Integral) :'Num = (fromInteger << toInteger) x

    let inline internal G0() = fromIntegral 0
    let inline internal G1() = fromIntegral 1

    let inline internal whenIntegral a = let _ = if false then toInteger a else 0I in ()

    let inline internal quot (a:'Integral) (b:'Integral) :'Integral = whenIntegral a; a / b
    let inline internal rem  (a:'Integral) (b:'Integral) :'Integral = whenIntegral a; a % b
    let inline internal quotRem a b :'Integral * 'Integral = (quot a b, rem a b)




    // Numeric Functions ------------------------------------------------------

    let inline internal gcd x y :'Integral =
        let zero = G0()
        let rec gcd' a = function
            | b when b = zero -> a
            | b -> gcd' b (rem a b)
        match(x,y) with
        | t when t = (zero,zero) -> failwith "Prelude.gcd: gcd 0 0 is undefined"
        | _                      -> gcd' (abs x) (abs y)


// Ratio ------------------------------------------------------------------
namespace FsControl.Core.NumericH98.Types
open FsControl.Core.Prelude
open FsControl.Core.NumericH98.TypeMethods
open FsControl.Core.NumericH98.TypeMethods.Numerics
open Num
open Integral

module Ratio = 
    let inline internal (</) x = (|>) x
    let inline internal (/>) x = flip x

    type Ratio<'Integral> = Ratio of 'Integral * 'Integral with
        override this.ToString() =
            let (Ratio(n,d)) = this
            n.ToString() + " % " + d.ToString()

    let inline internal ratio (a:'Integral) (b:'Integral) :Ratio<'Integral> =
        whenIntegral a
        let zero = G0()
        if b = zero then failwith "Ratio.%: zero denominator"
        let (a,b) = if b < zero then (negate a, negate b) else (a, b)
        let gcd = gcd a b
        Ratio (quot a gcd, quot b gcd)

    let inline internal Ratio (x,y) = x </ratio/> y

    let inline  internal numerator   (Ratio(x,_)) = x
    let inline  internal denominator (Ratio(_,x)) = x

    type Ratio<'Integral> with
        static member inline (/) (Ratio(a,b), Ratio(c,d)) = (a </opMultiply/> d) </ratio/> (b </opMultiply/> c)
                                              
        static member inline (+) (Ratio(a,b), Ratio(c,d)) = (a </opMultiply/> d </opPlus/>  c </opMultiply/> b) </ratio/> (b </opMultiply/> d)
        static member inline (-) (Ratio(a,b), Ratio(c,d)) = (a </opMultiply/> d </opMinus/> c </opMultiply/> b) </ratio/> (b </opMultiply/> d)
        static member inline (*) (Ratio(a,b), Ratio(c,d)) = (a </opMultiply/> c) </ratio/> (b </opMultiply/> d)

    type Ratio<'RA> with static member inline instance (Num.Abs        , r:Ratio<_>, _) = fun () -> (abs    (numerator r)) </ratio/> (denominator r)
    type Ratio<'RA> with static member inline instance (Num.Signum     , r:Ratio<_>, _) = fun () -> (signum (numerator r)) </ratio/> G1()
    type Ratio<'RA> with static member inline instance (dm:Num.FromInteger, _:Ratio<_>) = fun (x:bigint) -> Inline.instance Num.FromInteger x </ratio/> G1()
    type Ratio<'RA> with static member inline instance (Num.Negate     , r:Ratio<_>, _) = fun () -> -(numerator r) </ratio/> (denominator r)

type Rational = Ratio.Ratio<bigint>



namespace FsControl.Core.NumericH98.TypeMethods

open FsControl.Core.Prelude
open Num
open Integral
open Numerics
open FsControl.Core.NumericH98
open FsControl.Core.NumericH98.Types
open FsControl.Core.NumericH98.TypeMethods
open Ratio

open System.Numerics

// Fractional class -------------------------------------------------------
module Fractional =
    type FromRational = FromRational with
        static member        instance (FromRational, _:float   ) = fun (r:Rational) -> float   (numerator r) / float   (denominator r)
        static member        instance (FromRational, _:float32 ) = fun (r:Rational) -> float32 (numerator r) / float32 (denominator r)    
        static member        instance (FromRational, _:decimal ) = fun (r:Rational) -> decimal (numerator r) / decimal (denominator r)
        static member inline instance (FromRational, _:Ratio<_>) = fun (r:Rational) -> ratio (fromIntegral  (numerator r))  (fromIntegral (denominator r))
        static member        instance (FromRational, _:Complex ) = fun (r:Rational) -> Complex(float (numerator r) / float (denominator r), 0.0)


// RealFrac class ---------------------------------------------------------
module RealFrac =
    type ProperFraction = ProperFraction with
        static member        instance (ProperFraction, x:float   , _) = fun () -> let t = truncate x in (bigint (decimal t), opMinus x t)
        static member        instance (ProperFraction, x:float32 , _) = fun () -> let t = truncate x in (bigint (decimal t), opMinus x t)
        static member        instance (ProperFraction, x:decimal , _) = fun () -> let t = truncate x in (bigint          t , opMinus x t)
        static member inline instance (ProperFraction, r:Ratio<_>, _) = fun () -> 
            let (a,b) = (numerator r, denominator r)
            let (i,f) = quotRem a b
            (i, ratio f b)


// Real class -------------------------------------------------------------
module Real =
    let inline internal (</) x = (|>) x
    let inline internal (/>) x = flip x
    type ToRational = ToRational with
        static member inline instance (ToRational, r:Ratio<_>, _) = fun () -> toInteger (numerator r) </ratio/> toInteger (denominator r) :Rational
        static member inline instance (ToRational, x:'t      , _) = fun () ->
            let inline fromRational (x:Rational) :'Fractional = Inline.instance Fractional.FromRational x
            let inline whenFractional a = let _ = if false then fromRational (1I </ratio/> 1I) else a in () 
            whenFractional x
            let inline properFraction (x:'RealFrac) : 'Integral * 'RealFrac =
                let (a, b:'RealFrac) = Inline.instance (RealFrac.ProperFraction, x) ()
                (fromIntegral a, b)        
            let inline truncate (x:'RealFrac) :'Integral = fst <| properFraction x
            let (i:bigint,d) = properFraction x
            (i </ratio/> 1I) + (truncate (decimal d </opMultiply/> 1000000000000000000000000000M) </ratio/> 1000000000000000000000000000I) :Rational
        static member inline instance (ToRational, x:'t      , _) = fun () -> (toInteger x) </ratio/> 1I


// Floating class ---------------------------------------------------------
module Floating =
    type Pi = Pi with
        static member instance (Pi, _:float32) = fun () -> 3.14159274f
        static member instance (Pi, _:float  ) = fun () -> System.Math.PI
        static member instance (Pi, _:Complex) = fun () -> Complex(System.Math.PI, 0.0)

