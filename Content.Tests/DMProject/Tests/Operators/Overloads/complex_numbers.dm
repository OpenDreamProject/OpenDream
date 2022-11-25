/datum/complex     // complex number a+bi
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
        else if(isnum(C)) a += C

	proc/operator-()
		 a *= -1
		 b *= -1
		 return src
		 
    proc/operator*(datum/complex/C) 
		if(isnum(C))
			a *= C
			b *= C
			return src
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

/proc/RunTest()
	var/datum/complex/A = new /datum/complex(5,-1)
	var/datum/complex/B = new /datum/complex(0,-9.5)
	var/datum/complex/C = A*B
	world.log << "[C] = [C[1]] + [C[2]]i"
	
/proc/main()
	RunTest()