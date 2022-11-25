
/obj/proc/A()
	return 1

/obj/A()
	return 2

/obj/A()
	return 3

var/obj/a = new

/proc/RunTest()
	ASSERT(a.A() == 3)
