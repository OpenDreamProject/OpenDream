// Name - Expected Outcome


/obj/DerefTest1
    inner
        var/ele2 = 4

    var/ele = 2
    var/innerobj

    var/obj/DerefTest1/inner/innerobj_ty

    proc/inner_proc()
        return new /obj/DerefTest1
    proc/elefn()
        return 3

/world/proc/DerefTest1_new_obj()
    return new /obj/DerefTest1

// Deref 1 - No Error
/world/proc/DerefTest1_proc()
    ASSERT(world.DerefTest1_new_obj().ele == 2)
    ASSERT(world.DerefTest1_new_obj()?.ele == 2)

    ASSERT(world.DerefTest1_new_obj().inner_proc().ele == 2)
    ASSERT(world.DerefTest1_new_obj()?.inner_proc()?.ele == 2)

// Deref 2 - No Error
/world/proc/DerefTest2_proc()
    var/obj/DerefTest1/o2 = new /obj/DerefTest1
    ASSERT(o2.inner_proc().elefn() == 3)
    ASSERT(o2?.inner_proc()?.elefn() == 3)

    o2.innerobj_ty = new /obj/DerefTest1/inner
    ASSERT(o2:innerobj_ty.ele2 == 4)

// Deref 3 - No Error
/world/proc/DerefTest3_proc()
    var/list/l = newlist(/obj/DerefTest1)
    ASSERT(l[1].ele == 2)
    ASSERT(l[1].elefn() == 3)

    l[1].innerobj = new /obj/DerefTest1/inner
    ASSERT(l[1].innerobj.ele2 == 4)
