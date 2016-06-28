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

List.iter printfn [ "1"; "2" ]

let reverse list = 
    let rec aux acc = 
        function 
        | [] -> acc
        | h :: t -> aux (h :: acc) t
    aux [] list

let reverse_1 list = 
    let rec aux acc = 
        function 
        | [] -> acc
        | h :: t -> aux (h :: acc) t
    aux [] list

let reverse_2 list = 
    let aux = List.fold (fun state item -> (fun () -> item :: state())) (fun () -> []) list
    aux()

let reverse_3 list = List.fold (fun state item -> item :: state) [] list

let rec reverse_4 list = 
    match list with
    | [] -> list
    | h :: t -> reverse_4 t @ [ h ]

let printer x = printfn "%i" x

[ 1; 2; 3 ] |> List.iter printer

let replace oldStr newStr (s : string) = s.Replace(oldValue = oldStr, newValue = newStr)

"a" |> replace "a" "b"

let F w x y z = w x y z
let F1 x y z = x (y z)
let F2 x y z = y z |> x

let x = 
    let negate x = -x
    [ for i in 1..10 do
          if i % 2 = 0 then yield negate i
          else yield i ]

type point<'a> = 
    { x : float
      content : 'a }

let p = 
    { x = 4.2
      content = "" }

let p1 = 
    { x = 4.21
      content = "" }

let p3 = { p1 with content = "a" }

type application_started = 
    { id : System.Guid }

let rec remove_where filter list = 
    match list with
    | [] -> []
    | h :: t -> 
        if filter h then remove_where filter t
        else h :: remove_where filter t

let rec remove_where1 filter list = 
    match list with
    | [] -> []
    | h :: t -> 
        let new_t = remove_where1 filter t
        if filter h then new_t
        else h :: new_t

[ "Pipe"; "Forward" ] |> List.iter (fun s -> printfn "%s" s)
List.fold (*) 1 [ 1..10 ]
2
|> (/)
<| 1
[ "a" ] |> (fun s -> printfn "%d" s.Length)
printfn "The result of sprintf is %s" (sprintf "(%d, %d)" 1 2)

let rec take list = 
    match list with
    | [] -> []
    | h :: t -> h :: skip t

and skip list = 
    match list with
    | [] -> []
    | h :: t -> take t

let rec r = 
    function 
    | [] -> []
    | h :: t -> r t @ [ h ]

let rec merge x y = 
    match x, y with
    | [], _ -> y
    | _, [] -> x
    | xh :: xt, yh :: yt -> 
        if xh > yh then yh :: merge x yt
        else xh :: merge xt y

let rec split = 
    function 
    | [] -> ([], [])
    | h :: [] -> ([ h ], [])
    | h0 :: h1 :: t -> 
        let (x, y) = split (t)
        (h0 :: x, h1 :: y)

let rec merge_sort = 
    function 
    | [] -> []
    | [ h ] -> [ h ]
    | list -> 
        let (x, y) = split list
        let x = merge_sort x
        let y = merge_sort y
        merge x y


