/world/proc/DerefTest2_Proc()
    var/obj/DerefTest1/o2 = new /obj/DerefTest1
    ASSERT(o2.inner_proc().elefn() == 3)
    ASSERT(o2?.inner_proc()?.elefn() == 3)

    o2.innerobj_ty = new /obj/DerefTest1/inner
    ASSERT(o2:innerobj_ty.ele2 == 4)
