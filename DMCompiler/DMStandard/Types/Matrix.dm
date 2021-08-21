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

	proc/Add(matrix/Matrix2)
		if(!istype(Matrix2))
			CRASH("Invalid matrix")
		a += Matrix2.a
		b += Matrix2.b
		c += Matrix2.c
		d += Matrix2.d
		e += Matrix2.e
		f += Matrix2.f
		return src

	proc/Multiply(m)
		if(!istype(m, /matrix))
			return Scale(m)
		var/matrix/n = m
		var/old_a = a
		var/old_b = b
		var/old_c = c
		var/old_d = d
		var/old_e = e
		var/old_f = f

		a = old_a*n.a + old_d*n.b
		b = old_b*n.a + old_e*n.b
		c = old_c*n.a + old_f*n.b + n.c
		d = old_a*n.d + old_d*n.e
		e = old_b*n.d + old_e*n.e
		f = old_c*n.d + old_f*n.e + n.f
		return src

	proc/Scale(x, y)
		if(!isnum(x))
			x = 0
		if(!isnum(y))
			y = x
		a = a * x
		b = b * x
		c = c * x
		d = d * y
		e = e * y
		f = f * y
		return src

	proc/Subtract(matrix/Matrix2)
		if(!istype(Matrix2))
			CRASH("Invalid matrix")
		a -= Matrix2.a
		b -= Matrix2.b
		c -= Matrix2.c
		d -= Matrix2.d
		e -= Matrix2.e
		f -= Matrix2.f
		return src

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
