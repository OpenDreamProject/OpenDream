
//# issue 473

/obj
	var/a = 5

/proc/RunTest()
	var/obj/o1 = new /obj
	var/obj/o2 = new /obj{a=6}
	ASSERT(o1.a == 5)
	ASSERT(o2.a == 6)