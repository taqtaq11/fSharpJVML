let pow n x =
    begin
    let rec pow_ins x n s =
        begin
        if n > 0.0 then
            begin
            pow_ins(x)(n - 1.0)(s * x)
            end
        else
            begin
            s
            end
        end
    pow_ins(x)(n)(1.0)
    end

let avg x y =
    begin
    x / 2.0 + y / 2.0
    end

let sqrt x =
    begin
    let eps =
        begin
        0.0001
        end

    let abs a =
        begin
        let c = 
            begin 
            if a > 0.0 then 
                begin 
                    a; 
                end 
            else 
                begin 
                    0.0-a;
                end 
            end
        c;
        end

    let rec sqrt_ins guess x =
        begin
        if abs(guess * guess - x) < eps then
            begin
            guess
            end
        else
            begin
            sqrt_ins (avg(guess)(x/guess))(x)
            end
        end
    sqrt_ins(1.0)(x)
    end

let hypotenuse a b =    
    begin
    let square =
        begin
        pow(2.0)
        end

    sqrt(square(a) + square(b))
    end

let main argv =
    begin
    printf ("%d") (hypotenuse(6.0)(8.0));
    0;
    end