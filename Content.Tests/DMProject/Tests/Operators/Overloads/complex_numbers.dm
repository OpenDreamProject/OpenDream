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

	proc/operator-(datum/complex/C)
		if(isnull(C)) return new /datum/complex(a*-1, b*-1)
		if(istype(C)) return new /datum/complex(a-C.a, b-C.b)
		if(isnum(C)) return new /datum/complex(a-C, b)
		 
	proc/operator*(datum/complex/C) 
		if(isnum(C))
			return new /datum/complex(a*C, b*C)
		else
			return new /datum/complex((src.a * C.a) - (src.b * C.b), (src.a * C.b) + (src.b * C.a))

	proc/operator/=(datum/complex/C)
		if(isnum(C))
			return new /datum/complex(a/C, b/C)
		else
			return new /datum/complex((src.a * C.a) - (src.b * C.b), (src.a * C.b) + (src.b * C.a))
	
	proc/operator|(datum/complex/C)
		//nonsense, used for testing
		src.a = C.a

	proc/operator[](index) 
		switch(index)
			if(1)
				return src.a
			if(2)
				return src.b
			else
				throw EXCEPTION("Invalid index on complex number")

	proc/operator[]=(index, value) 
		switch(index)
			if(1)
				src.a = value
			if(2)
				src.b = value
			else
				throw EXCEPTION("Invalid index assign on complex number [index] = [value]")				


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
	//test implemented
	//+
	var/datum/complex/result = A + B
	ASSERT(result.a == 8 && result.b == -10.5)
	result = A + 5
	ASSERT(result.a == 10 && result.b == -1)
	//-
	result = A - B
	ASSERT(result.a == 2 && result.b == 8.5)
	result = A - 5
	ASSERT(result.a == 0 && result.b == -1)

	//Unary operators need their own proc, it's not just subtract(null)
	//result = -A
	//ASSERT(result.a == -5 && result.b == 1)
	//*
	result = A * B
	ASSERT(result.a == 5.5 && result.b == -50.5)
	result = A * 2
	ASSERT(result.a == 10 && result.b == -2)
	//|
	result = A | B
	ASSERT(result.a == 3 && result.b == -1)

	result *= 2
	ASSERT(result.a == 6 && result.b == -2)

	result /= 2
	ASSERT(result.a == 3 && result.b == -1)

	//[] and []=
	ASSERT(result[1] == 3)
	ASSERT(result[2] == -1)
	ASSERT(result[1] == 3)
	ASSERT(result[2] == -1)
	ASSERT(result[1] == 3)
	ASSERT(result[2] == -1)
	var/test = result[1]
	test *= result[2]
	ASSERT(test == -3)

	ASSERT(result.a == 3 && result.b == -1)