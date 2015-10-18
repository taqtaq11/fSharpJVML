let abs a =
    let c = if a > 0 then a else -a
    c

[<EntryPoint>]
let main argv =
    printf "%d\n" (abs 6)
    0