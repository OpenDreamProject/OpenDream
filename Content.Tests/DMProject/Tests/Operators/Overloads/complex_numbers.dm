/datum/complex	 // complex number a+bi
	var/a as num
	var/b as num

	New(_a,_b)
		a = _a
		b = _b

	proc/operator+(datum/complex/C)
		if(istype(C)) return new /datum/complex(a+C.a, b+C.b)
		if(isnum(C)) return new /datum/complex(a+C, b)
		return src

	proc/operator+=(datum/complex/C)
		if(istype(C))
			a += C.a
			b += C.b
		else if(isnum(C)) 
			a += C

	proc/operator-()
		return new /datum/complex(a*-1, b*-1)
		 
	proc/operator*(datum/complex/C) 
		if(isnum(C))
			return new /datum/complex(a*C, b*C)
		else
			return new /datum/complex((src.a * C.a) - (src.b * C.b), (src.a * C.b) + (src.b * C.a))

	proc/operator[](index) 
		switch(index)
			if(1)
				return src.a
			if(2)
				return src.b
			else
				throw EXCEPTION("Invalid index on complex number")


	proc/operator&(datum/complex/C)
		return new /datum/complex(src.a & C.a, src.b & C.b)

	proc/operator&=(datum/complex/C)
		src.a = 3.14
		src.b = 6.28

	proc/operator:=(datum/complex/C)
		return new /datum/complex(3.14,8888)		


/proc/RunTest()
	var/datum/complex/A = new /datum/complex(5,-1)
	var/datum/complex/B = new /datum/complex(3,-9.5)
	//Just compile for now