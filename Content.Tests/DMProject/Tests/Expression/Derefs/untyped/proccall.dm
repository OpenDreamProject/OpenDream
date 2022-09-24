// COMPILE ERROR

/obj
    proc/elefn()
        return 3

/proc/RunTest()
    var/o = new
    ASSERT(o.elefn() == 3)
