let join separator =
    let _separate s1 s2 :string = s1 + separator + s2
    List.reduce _separate