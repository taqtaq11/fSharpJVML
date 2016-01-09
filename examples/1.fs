let rec factorial s n:int =
    begin
    if n > 0 then
        begin
        factorial(s * n)(n - 1);
        end
    elif n = 0 then
        begin
        0;
        end
    else
        begin
        s;
        end
    end

let main argv =
    begin
    printf ("%d") (factorial (1) (4));
    0;
    end