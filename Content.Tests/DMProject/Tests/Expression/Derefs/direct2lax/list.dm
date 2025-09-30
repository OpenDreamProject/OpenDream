
//# issue 114

/datum
    var/ele = 2
    proc/elefn()
        return 4

/proc/RunTest()
    var/list/l = newlist(/datum)
    ASSERT(l[1].ele == 2)
    ASSERT(l[1].elefn() == 4)
