// COMPILE ERROR

/proc/novar(a, b)
    return a + b

/proc/RunTest()
    var/test = (novar(var/c, var/d))
