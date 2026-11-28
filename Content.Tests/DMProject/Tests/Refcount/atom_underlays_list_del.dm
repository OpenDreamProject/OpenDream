// RUNTIME ERROR

// An atom's underlays list can't be deleted on its own
/proc/RunTest()
	var/obj/O = new
	del(O.underlays)
