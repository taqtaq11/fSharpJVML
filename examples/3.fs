let abs a =
    begin
    let c = 
        begin 
            if a > 0 then 
                begin 
                    a; 
                end 
            else 
                begin 
                    0-a;
                end 
        end
    c;
    end

let main argv =
    begin
    printf ("%d\n") (abs (6));
    0;
    end;