/matrix
	parent_type = /datum

	var/a = 1
	var/b = 0
	var/c = 0
	var/d = 0
	var/e = 1
	var/f = 0

	New(var/a = 1, var/b = 0, var/c = 0, var/d = 0, var/e = 1, var/f = 0)
		if (istype(a, /matrix))
			var/matrix/mat = a
			src.a = mat.a
			src.b = mat.b
			src.c = mat.c
			src.d = mat.d
			src.e = mat.e
			src.f = mat.f
		else
			src.a = a
			src.b = b
			src.c = c
			src.d = d
			src.e = e
			src.f = f
	
	proc/Translate(x, y = x)
		c += x
		f += y

	proc/Turn(angle)
		var/angleCos = cos(angle)
		var/angleSin = sin(angle)

		a = a * angleCos + b * angleSin
		d = d * angleCos + e * angleSin
		e = a * -angleSin + e * angleCos
		b = d * -angleSin + b * angleCos

proc/matrix(var/a, var/b, var/c, var/d, var/e, var/f)
	return new /matrix(a, b, c, d, e, f)