
//# issue 471

/mob
	proc/dodir()
		var/out = 0
		dir = NORTH
		for(dir in list(1,2,3,4)) 
			out += dir
		out += dir
		ASSERT(out == 14)

/datum/unit_test/nonlocal_var/RunTest()
	var/mob/m = new
	m.dodir()
