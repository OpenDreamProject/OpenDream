
//# issue 606

/obj
	var/list/L = list(1,2,3,4,5)

/proc/RunTest()
	var/obj/o = new
	ASSERT(initial(o.L[3]) == 3)
	o.L[3] = 6
	var/idx = 3
	ASSERT(initial(o.L[idx]) == 6) // initial() on almost any list index (except vars) returns the current value
