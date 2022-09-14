
//# issue 557

/obj
	var/a = 5
	proc/setsrc()
		ASSERT(src.a == 5)
		src = 7
		ASSERT(src == 7)

/proc/RunTest()
	var/obj/o = new
	o.setsrc()