/matrix
	parent_type = /datum

	var/a = 1
	var/b = 0
	var/c = 0
	var/d = 0
	var/e = 1
	var/f = 0

	proc/Interpolate(Matrix2, t)
		set opendream_unimplemented = TRUE

	New(var/a, var/b, var/c, var/d, var/e, var/f)

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

	proc/Invert()

	proc/Multiply(m)

	proc/Scale(x, y)

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


proc/matrix(var/a, var/b, var/c, var/d, var/e, var/f)
