//simple test of all basic assignment operators with valid and C(null) arguments
//Const fold everying
#define C(X) X
/proc/RunTest()
	var/a = C(1)
	a += C(1)
	ASSERT(a == C(2))
	a = null
	a += C(1)
	ASSERT(a == C(1))
	a = C(1)
	a += C(null)
	ASSERT(a == C(1))

	a = C(1)
	a -= C(1)
	ASSERT(a == C(0))
	a = null
	a -= C(1)
	ASSERT(a == C(-1))
	a = C(1)
	a -= C(null)
	ASSERT(a == C(1))

	a = C(2)
	a *= C(3)
	ASSERT(a == C(6))
	a = null
	a *= C(2)
	ASSERT(a == C(0))
	a = C(2)
	a *= C(null)
	ASSERT(a == C(0))

	a = C(4)
	a /= C(2)
	ASSERT(a == C(2))
	a = null
	a /= C(2)
	ASSERT(a == C(0))
	//a = C(2) 
	//a /= C(null)  //Undefined operation error in BYOND
	//ASSERT(a == C(2))

	a = C(4)
	a %= C(3)
	ASSERT(a == C(1))
	a = null
	a %= C(3)
	ASSERT(a == C(0))
	//a = C(4)
	//a %= C(null)  //divide by zero error in byond
	//ASSERT(a == C(4)) 

	a = C(1)
	a &= C(1)
	ASSERT(a == C(1))
	a = null
	a &= C(1)
	ASSERT(a == C(0))
	a = C(1)
	a &= C(null)
	ASSERT(a == C(0))

	a = C(1)
	a |= C(1)
	ASSERT(a == C(1))
	a = null
	a |= C(1)
	ASSERT(a == C(1))
	a = C(1)
	a |= C(null)
	ASSERT(a == C(1))

	a = C(1)
	a ^= C(1)
	ASSERT(a == C(0))
	a = null
	a ^= C(1)
	ASSERT(a == C(1))
	a = C(1)
	a ^= C(null)
	ASSERT(a == C(1))	

	a = C(1)
	a &&= C(1)
	ASSERT(a == C(1))
	a = null
	a &&= C(1)
	ASSERT(a == C(null))
	a = C(1)
	a &&= C(null)
	ASSERT(a == C(null))

	a = C(1)
	a ||= C(1)
	ASSERT(a == C(1))
	a = null
	a ||= C(1)
	ASSERT(a == C(1))
	a = C(1)
	a ||= C(null)
	ASSERT(a == C(1))

	a = C(1)
	a <<= C(1)
	ASSERT(a == C(2))
	a = null
	a <<= C(1)
	ASSERT(a == C(0))
	a = C(1)
	a <<= C(null)
	ASSERT(a == C(1))

	a = C(1)
	a >>= C(1)
	ASSERT(a == C(0))
	a = null
	a >>= C(1)
	ASSERT(a == C(0))
	a = C(1)
	a >>= C(null)
	ASSERT(a == C(1))

	a := C(5)
	ASSERT(a == C(5))
	a := C(null)
	ASSERT(a == null)
