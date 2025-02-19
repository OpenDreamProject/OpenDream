/vector
	var/len as num
	var/size as num
	var/x as num
	var/y as num
	var/z as num

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

/proc/vector(x, y, z) as /vector
	return new /vector(x, y, z)