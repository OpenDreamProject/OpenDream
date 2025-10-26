
/datum
    var/datum/inner/o2

/datum/inner
    var/ele = 2
    proc/elefn()
        return 4

/proc/RunTest()
    var/list/l = newlist(/datum)
    l[1].o2 = new /datum/inner
    ASSERT(l[1].o2.ele == 2)
    ASSERT(l[1].o2.elefn() == 4)