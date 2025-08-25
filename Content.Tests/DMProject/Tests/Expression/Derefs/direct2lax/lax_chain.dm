/datum
    var/datum/inner/o2

/datum/inner
    var/ele = 2
    proc/elefn()
        return 4

/proc/RunTest()
    var/datum/o = new
    o.o2 = new /datum/inner
    ASSERT(o:o2.ele == 2)
    ASSERT(o:o2.elefn() == 4)