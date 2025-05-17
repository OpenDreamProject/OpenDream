/vector
	var/len = null as num
	var/size = null as num
	var/x = 0 as num
	var/y = 0 as num
	var/z = 0 as num

	proc/New(x, y, z)
	
	proc/Cross(vector/B)
		set opendream_unimplemented = TRUE
	
	proc/Dot(vector/B)
		return x * B.x + y * B.y + z * B.z
	
	proc/Interpolate(vector/B, t)
		return src + (B-src) * t
	
	proc/Normalize()
		size = 1
		return src
	
	proc/Turn(angle)
		set opendream_unimplemented = TRUE

/proc/vector(x, y, z)
	return new /vector(x, y, z)
