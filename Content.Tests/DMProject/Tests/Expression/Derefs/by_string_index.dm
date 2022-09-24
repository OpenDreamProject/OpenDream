
/obj
    var/a = 5

/proc/RunTest()
    var/obj/o = new
    ASSERT(o.a == 5)
    o.vars["a"] = 10
    ASSERT(o.a == 10)
