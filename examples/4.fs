let f x y =
    begin
    x + y;
    end

let main argv =
    begin
    let g = begin f (5); end
    printf ("%i") (g (6));
    printf ("%i") (g (19));
    0;
    end