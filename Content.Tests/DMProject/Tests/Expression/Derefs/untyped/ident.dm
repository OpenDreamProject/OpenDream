// COMPILE ERROR

/obj
    var/ele = 2

/proc/RunTest()
    var/o = new
    ASSERT(o.ele == 2)