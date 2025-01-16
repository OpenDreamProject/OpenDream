
//# issue 473

/obj
	var/a = 5
	var/b = 7

/proc/RunTest()
	var/obj/o1 = new /obj
	var/obj/o2 = new /obj{a=6;b=8}
	ASSERT(o1.a == 5)
	ASSERT(o1.b == 7)
	ASSERT(o2.a == 6)
	ASSERT(o2.b == 8)