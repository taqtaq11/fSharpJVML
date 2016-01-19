let rec fib n =
    begin
    if n > 1 then
        begin
        fib(n - 2) + fib(n - 1);
        end
    elif n = 1 then
        begin
        1
        end
    else
        begin
        0
        end
    end

let main argv =
    begin
    printf ("%i") (fib (5));
    0;
    end