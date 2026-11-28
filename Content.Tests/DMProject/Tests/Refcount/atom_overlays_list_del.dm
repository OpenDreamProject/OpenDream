// RUNTIME ERROR

// An atom's overlays list can't be deleted on its own
/proc/RunTest()
	var/obj/O = new
	del(O.overlays)
