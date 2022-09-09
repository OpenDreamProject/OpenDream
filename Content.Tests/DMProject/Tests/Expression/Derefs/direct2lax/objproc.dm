
/obj
    var/ele = 2
    proc/elefn()
        return 4
    proc/new_new_obj()
        return new /obj

/proc/new_obj()
    return new /obj

/proc/RunTest()
    var/obj/o = new
    ASSERT(o.new_new_obj().ele == 2)
    ASSERT(o?.new_new_obj()?.ele == 2)
    ASSERT(o.new_new_obj().elefn() == 4)
    ASSERT(o?.new_new_obj()?.elefn() == 4)