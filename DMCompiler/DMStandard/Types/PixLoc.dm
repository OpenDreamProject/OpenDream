/pixloc
	var/turf/loc as opendream_unimplemented
	var/step_x as opendream_unimplemented
	var/step_y as opendream_unimplemented
	var/x as num|opendream_unimplemented
	var/y as num|opendream_unimplemented
	var/z as num|opendream_unimplemented

	proc/New(x, y, z)
		set opendream_unimplemented = TRUE

/proc/pixloc(x, y, z)
	return new /pixloc(x, y, z)
