// RUNTIME ERROR

// An atom's filters list can't be deleted on its own
/proc/RunTest()
	var/obj/O = new
	del(O.filters)
