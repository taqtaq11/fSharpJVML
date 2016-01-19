let f x =
    begin
    x * 2
    end

let g fn y =
    begin
    fn(y)
    end

let main args =
    begin
    printf ("%i") (g(f)(5)); 
    0
    end