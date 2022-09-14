
//# issue 609

/obj
	var/list/L = list(1,2,3,4,5)

/proc/RunTest()
	var/obj/o = new
	ASSERT(issaved(o.L[3]) == FALSE)