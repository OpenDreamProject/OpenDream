/proc/matrix_reject(bad, position)
    var/a = 1
    var/b = 2
    var/c = 3
    var/d = 4
    var/e = 5
    var/f = 6
    switch(position)
        if(0)
            a = bad
            b = bad
            c = bad
            d = bad
            e = bad
            f = bad
        if(1) a = bad
        if(2) b = bad
        if(3) c = bad
        if(4) d = bad
        if(5) e = bad
        if(6) f = bad
        else
            return -1
    try
        var/matrix/result = matrix(a, b, c, d, e, f)
        result = null
    catch(var/exception/failure)
        if(findtext(failure.name, "Invalid arguments used to create matrix") != 1)
            return -2
        return 1
    return 0

/proc/matrix_success()
    var/matrix/identity = matrix()
    if(identity.a != 1 || identity.b != 0 || identity.c != 0 || identity.d != 0 || identity.e != 1 || identity.f != 0)
        return 0
    var/matrix/input = matrix(-1, 0, 3, 4, 5, 6)
    var/matrix/copy = matrix(input)
    if(copy == input || copy.a != -1 || copy.b != 0 || copy.c != 3 || copy.d != 4 || copy.e != 5 || copy.f != 6)
        return 0
    copy.a = 17
    if(input.a != -1)
        return 0
    var/matrix/first_matrix = matrix(input, 9, 9, 9, 9, 9)
    if(first_matrix.a != input.a || first_matrix.f != input.f)
        return 0
    identity = null
    input = null
    copy = null
    first_matrix = null
    return 1
