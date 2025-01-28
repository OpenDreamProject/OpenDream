
//# issue 693

/obj
	var/static/list/A1 = list(1,2)
	var/static/list/A2 = list(1,2)
	var/static/list/A3 = list(1,2)
	var/static/list/B = A1 + A2 + A3

/proc/RunTest()
	var/obj/o = new
	ASSERT(o.B.len == 6)
