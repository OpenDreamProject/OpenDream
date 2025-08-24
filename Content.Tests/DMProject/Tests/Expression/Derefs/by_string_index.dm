
/datum
    var/a = 5

/proc/RunTest()
    var/datum/o = new
    ASSERT(o.a == 5)
    o.vars["a"] = 10
    ASSERT(o.a == 10)
