
//# issue 666

/obj/o
	var/test = 5

/proc/get_obj()
	return (new /obj/o)

/proc/RunTest()
	var/obj/o/thing
	var/c = (thing = get_obj()).test
	ASSERT(c == 5)