// RUNTIME ERROR

// An atom's vis_contents list can't be deleted on its own
/proc/RunTest()
	var/obj/O = new
	del(O.vis_contents)
