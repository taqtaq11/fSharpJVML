let rec fib n =
    if n > 1 then
        fib (n - 2) + fib(n - 1)
    else
        1

[<EntryPoint>]
let main argv =
    printf "%d\n" (fib 5)
    0