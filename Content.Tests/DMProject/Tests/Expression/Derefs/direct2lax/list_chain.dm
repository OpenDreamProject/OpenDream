
/obj
    var/obj/inner/o2

/obj/inner
    var/ele = 2
    proc/elefn()
        return 4

/proc/RunTest()
    var/list/l = newlist(/obj)
    l[1].o2 = new /obj/inner
    ASSERT(l[1].o2.ele == 2)
    ASSERT(l[1].o2.elefn() == 4)