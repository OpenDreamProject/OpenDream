/obj
    var/obj/inner/o2

/obj/inner
    var/ele = 2
    proc/elefn()
        return 4

/proc/RunTest()
    var/obj/o = new
    o.o2 = new /obj/inner
    ASSERT(o:o2.ele == 2)
    ASSERT(o:o2.elefn() == 4)