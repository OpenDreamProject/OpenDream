
/obj
	var/a = 5
	proc/a()
		ASSERT(a == 5)
		return 5

/proc/RunTest()
	var/obj/o = new
	ASSERT(o.a() == 5)
