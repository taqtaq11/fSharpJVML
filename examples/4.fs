let f x y =
    begin
    x + y;
    end

let main argv =
    begin
    let g = begin f ("1"); end
    printf ("%s") (g ("2"));
    printf ("%s") (g ("5"));
    0;
    end