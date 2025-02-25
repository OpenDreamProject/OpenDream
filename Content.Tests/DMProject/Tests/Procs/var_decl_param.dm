// COMPILE ERROR OD0001

/proc/novar(a, b)
    return a + b

/proc/RunTest()
    var/test = (novar(var/c, var/d))
