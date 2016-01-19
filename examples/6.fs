let f x y = 
    begin
    let l =
        begin
        8.9
        end

    let g a =
        begin

        let h o oo =
            begin
            a + o + oo + l - x
            end

        h(a)(7.1) + y
        end

    let g2 a =
        begin
        g(a)
        end

    g2(x)
    end

let main args =
    begin
    printf ("%d") (f(1.2)(2.9)); 
    0
    end