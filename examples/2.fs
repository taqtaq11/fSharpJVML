let rec fib n =
    begin
    if n > 1 then
        begin
        fib(n - 2) + fib(n - 1);
        end
    else
        begin
        1;
        end
    end

let main argv =
    begin
    printf ("%d\n") (fib (5));
    0;
    end