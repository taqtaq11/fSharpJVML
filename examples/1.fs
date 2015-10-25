let rec factorial s n =
    begin
    if n > 0 then
        begin
        factorial(s * n)(n - 1);
        end
    else
        begin
        s;
        end
    end

let main argv =
    begin
    printf ("%d\n") (factorial (1) (4));
    0;
    end