/world/proc/DerefTest3_Proc()
    var/list/l = newlist(/obj/DerefTest1)
    ASSERT(l[1].ele == 2)
    ASSERT(l[1].elefn() == 3)

    l[1].innerobj = new /obj/DerefTest1/inner
    ASSERT(l[1].innerobj.ele2 == 4)
