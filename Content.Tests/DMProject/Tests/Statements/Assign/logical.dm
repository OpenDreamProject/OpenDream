
//# issue 612

/obj
	var/a = 1

var/obj/o

/proc/RunTest()
	o ||= new()
	ASSERT(o.a)
