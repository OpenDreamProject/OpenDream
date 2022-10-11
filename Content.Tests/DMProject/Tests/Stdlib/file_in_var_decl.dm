
//# issue 689

/obj
	var/a = file("test.dm")

/proc/RunTest()
	var/obj/o = new
	ASSERT(isfile(o.a))
