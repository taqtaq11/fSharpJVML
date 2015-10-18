let f x y =
    x + y

[<EntryPoint>]
let main argv =
    let g = f 5
    printf "%d" (g 6)
    0