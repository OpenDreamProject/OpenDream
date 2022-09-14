
//# issue 598

/obj
	var/list/v = list(1,2)

/proc/RunTest()
	var/obj/o = new
	o.v ||= list(3,4)
	ASSERT(o.v[1] == 1)