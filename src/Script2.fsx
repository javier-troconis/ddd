module MathStuff =
    let add x y = x + y
    type IntegerFn = int -> int -> int

module Customer = 

    // Customer.T is the primary type for this module
    type T = {AccountId:int; Name:string}

    // constructor
    let create id name = 
        {AccountId=id; Name=name}

    // method that works on the type
    let isValid {T.AccountId=id; } = 
        id > 0

    type customer = {name:string}

    type loadCustomer = int -> customer

    



