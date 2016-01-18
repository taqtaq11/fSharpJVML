let rec factorial s n:int =
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
    printf ("%i") (factorial (1) (6));
    0;
    end