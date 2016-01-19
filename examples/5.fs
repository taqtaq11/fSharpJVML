let main argv =
    begin
    let f1 = 
        begin
            fun (a:int) -> 
                begin
                a * 2;
                end
        end

    let res = begin f1(5); end
    printf ("%i") (res);
    0;
    end