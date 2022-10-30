//# issue 616

/obj
	var/const/b = 5

/proc/RunTest()
	var/obj/o = new
	var/const/v = o.b
	ASSERT(v == 5)
