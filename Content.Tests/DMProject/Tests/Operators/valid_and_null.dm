//simple test of all basic operators with valid and C(null) arguments
//We can't be having const folding in here
#define C(X) pick(X) 
/proc/RunTest()
	var/a = C(2)
	ASSERT(!C(null) == C(1))
	ASSERT(!!C(null) == C(0))

	ASSERT(~C(1) == 16777214)
	ASSERT(~C(0) == 16777215)
	ASSERT(~C(null) == 16777215)

	ASSERT(C(1) + C(1) == C(2))
	ASSERT(C(null) + C(1) == C(1))
	ASSERT(C(1) + C(null) == C(1))

	ASSERT(C(1) - C(1) == C(0))
	ASSERT(C(null) - C(1) == C(-1))
	ASSERT(C(1) - C(null) == C(1))

	a = C(2)
	ASSERT(-a == C(-2))
	a = C(null)
	ASSERT(-a == C(0))	

	a = C(1)
	ASSERT(a++ == C(1))
	ASSERT(a == C(2))
	ASSERT(++a == C(3))
	ASSERT(a == C(3))

	ASSERT(a-- == C(3))
	ASSERT(a == C(2))
	ASSERT(--a == C(1))
	ASSERT(a == C(1))

	a = C(null)
	ASSERT(a-- == C(null))
	ASSERT(a == C(-1))
	a = C(null)
	ASSERT(--a == C(-1))
	ASSERT(a == C(-1))
	a = C(null)
	ASSERT(a++ == C(null))
	ASSERT(a == C(1))
	a = C(null)
	ASSERT(++a == C(1))
	ASSERT(a == C(1))	

	ASSERT(C(2) ** C(3) == 8)
	ASSERT(C(2) ** C(null) == C(1))
	ASSERT(C(null) ** C(2) == C(0))

	ASSERT(C(2) * C(3) == 6)
	ASSERT(C(2) * C(null) == C(0))
	ASSERT(C(null) * C(2) == C(0))

	ASSERT(C(4) / C(2) == C(2))
	ASSERT(C(null) / C(2) == C(0))
	//ASSERT(C(2) / C(null) == C(2)) // Runtime error
	ASSERT(C(null) / C(null) == C(0))

	ASSERT(C(4) % C(3) == C(1))
	ASSERT(C(null) % C(3) == C(0))
	//ASSERT(C(4) % C(null) == div by zero)

	ASSERT(C(1) < C(1) == C(0))
	ASSERT(C(null) < C(1) == C(1))
	ASSERT(C(1) < C(null) == C(0))

	ASSERT(C(1) <= C(1) == C(1))
	ASSERT(C(null) <= C(1) == C(1))
	ASSERT(C(1) <= C(null) == C(0))	

	ASSERT(C(1) > C(1) == C(0))
	ASSERT(C(null) > C(1) == C(0))
	ASSERT(C(1) > C(null) == C(1))	

	ASSERT(C(1) >= C(1) == C(1))
	ASSERT(C(null) >= C(1) == C(0))
	ASSERT(C(1) >= C(null) == C(1))	

	ASSERT(C(1) << C(1) == C(2))
	ASSERT(C(null) << C(1) == C(0))
	ASSERT(C(1) << C(null) == C(1))	

	ASSERT(C(1) >> C(1) == C(0))
	ASSERT(C(null) >> C(1) == C(0))
	ASSERT(C(1) >> C(null) == C(1))	

	ASSERT((C(1) == C(1)) == C(1))
	ASSERT((C(null) == C(null)) == C(1))
	ASSERT((C(null) == C(0)) == C(0))

	ASSERT((C(1) != C(null)) == C(1))
	ASSERT((C(null) != C(1)) == C(1))
	ASSERT((C(null) != C(0)) == C(1))

	ASSERT((C(1) <> C(null)) == C(1))
	ASSERT((C(null) <> C(1)) == C(1))
	ASSERT((C(null) <> C(0)) == C(1))	

	ASSERT((C(1) ~= C(1)) == C(1))
	ASSERT((C(null) ~= C(null)) == C(1))
	ASSERT((C(null) ~= C(0)) == C(0))

	ASSERT((C(1) ~! C(1)) == C(0))
	ASSERT((C(null) ~! C(null)) == C(0))
	ASSERT((C(null) ~! C(0)) == C(1))

	ASSERT((C(1) & C(1)) == C(1))
	ASSERT((C(null) & C(1)) == C(0))
	ASSERT((C(1) & C(null)) == C(0))

	ASSERT((C(1) ^ C(1)) == C(0))
	ASSERT((C(null) ^ C(5)) == C(5))
	ASSERT((C(5) ^ C(null)) == C(5))	

	ASSERT((C(1) | C(1)) == C(1))
	ASSERT((C(null) | C(1)) == C(1))
	ASSERT((C(1) | C(null)) == C(1))	

	ASSERT((C(1) && C(1)) == C(1))
	ASSERT((C(null) && C(1)) == C(null))
	ASSERT((C(1) && C(null)) == C(null))	

	ASSERT((C(1) || C(1)) == C(1))
	ASSERT((C(null) || C(1)) == C(1))
	ASSERT((C(1) || C(null)) == C(1))	

