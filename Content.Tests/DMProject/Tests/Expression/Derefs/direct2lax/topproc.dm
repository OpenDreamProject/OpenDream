
//# issue 114

/obj
    var/ele = 2
    proc/elefn()
        return 4

/proc/new_obj()
    return new /obj

/proc/RunTest()
    ASSERT(new_obj().ele == 2)
    ASSERT(new_obj()?.ele == 2)
    ASSERT(new_obj().elefn() == 4)
    ASSERT(new_obj()?.elefn() == 4)