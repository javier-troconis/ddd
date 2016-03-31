module Script1

let rec last = 
    function 
    | [] -> None
    | [ x ] -> Some x
    | _ :: r -> last (r)

let rec sum = 
    function 
    | 0 -> 0
    | n -> n + sum (n - 1)

let rec destutter list = 
    match list with
    | [] -> []
    | [ hd ] -> [ hd ]
    | hd1 :: hd2 :: tl -> 
        if hd1 = hd2 then destutter (hd2 :: tl)
        else hd1 :: destutter (hd2 :: tl)

let log_entry maybe_time message = 
    let time = 
        match maybe_time with
        | Some x -> x
        | None -> System.DateTime.Now.ToLongDateString()
    time + "-" + message

type circle = 
    { radius : float }

type line = 
    { length : float }

type scene_element = 
    | Circle of circle
    | Line of line

let languages = "a,b,c"

let dashed_languages = 
    let language_list = languages.Split ','
    String.concat "-" language_list

let rec find_first_stutter list = 
    match list with
    | [] | [ _ ] -> None
    | first :: second :: rest -> 
        if first = second then Some first
        else find_first_stutter (second :: rest)

let rec is_even x = 
    if x = 0 then true
    else is_odd (x - 1)
and is_odd x = 
    if x = 0 then false
    else is_even (x - 1)
