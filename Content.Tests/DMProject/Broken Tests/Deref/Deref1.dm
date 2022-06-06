/world/proc/DerefTest1_new_obj()
    return new /obj/DerefTest1

/world/proc/Deref1_Proc()
    ASSERT(world.DerefTest1_new_obj().ele == 2)
    ASSERT(world.DerefTest1_new_obj()?.ele == 2)

    ASSERT(world.DerefTest1_new_obj().inner_proc().ele == 2)
    ASSERT(world.DerefTest1_new_obj()?.inner_proc()?.ele == 2)
