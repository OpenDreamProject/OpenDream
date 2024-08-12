// COMPILE ERROR

/obj
    var/ele = 2

/proc/RunTest()
    var/o = new /obj
    ASSERT(o.ele == 2)