
//# issue 114

/obj
    var/ele = 2
    proc/elefn()
        return 4

/proc/RunTest()
    var/list/l = newlist(/obj)
    ASSERT(l[1].ele == 2)
    ASSERT(l[1].elefn() == 4)
