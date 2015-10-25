let f x y =
    begin
    x + y;
    end

let main argv =
    begin
    let g = begin f (5); end
    printf ("%d") (g (6));
    0;
    end