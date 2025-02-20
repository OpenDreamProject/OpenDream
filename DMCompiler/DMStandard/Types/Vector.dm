/vector
	// TODO: Verify these default values
	var/len = 2 as num
	var/size = 0 as num
	var/x = 0 as num
	var/y = 0 as num
	var/z = 0 as num

	proc/New(x, y, z)
	
	proc/Cross(vector/B)
		set opendream_unimplemented = TRUE
	
	proc/Dot(vector/B)
		set opendream_unimplemented = TRUE
	
	proc/Interpolate(vector/B, t)
		set opendream_unimplemented = TRUE
	
	proc/Normalize()
		set opendream_unimplemented = TRUE
	
	proc/Turn(angle)
		set opendream_unimplemented = TRUE

/proc/vector(x, y, z)
	return new /vector(x, y, z)
