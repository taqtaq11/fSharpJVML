let rec factorial s n =
    if n > 0 then
        factorial (s * n) (n - 1)
    else
        s

[<EntryPoint>]
let main argv =
    printf "%d\n" (factorial 1 4)
    0