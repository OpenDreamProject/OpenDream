//simple test of all basic operators with valid and null arguments
/proc/RunTest()
	var/a = 2
	ASSERT(!null == 1)
	ASSERT(!!null == 0)

	ASSERT(~1 == 16777214)
	ASSERT(~0 == 16777215)
	ASSERT(~null == 16777215)

	a = 2
	ASSERT(-a == -2)
	a = null
	ASSERT(-a == 0)	

	a = 1
	ASSERT(a++ == 1)
	ASSERT(a == 2)
	ASSERT(++a == 3)
	ASSERT(a == 3)

	ASSERT(a-- == 3)
	ASSERT(a == 2)
	ASSERT(--a == 1)
	ASSERT(a == 1)

	a = null
	ASSERT(a-- == null)
	ASSERT(a == -1)
	a = null
	ASSERT(--a == -1)
	ASSERT(a == -1)
	a = null
	ASSERT(a++ == null)
	ASSERT(a == 1)
	a = null
	ASSERT(++a == 1)
	ASSERT(a == 1)	

	ASSERT(2 ** 3 == 8)
	ASSERT(2 ** null == 1)
	ASSERT(null ** 2 == 0)

	ASSERT(2 * 3 == 6)
	ASSERT(2 * null == 0)
	ASSERT(null * 2 == 0)

	ASSERT(4 / 2 == 2)
	ASSERT(null / 2 == 0)
	ASSERT(2 / null == 2)

	ASSERT(4 % 3 == 1)
	ASSERT(null % 3 == 0)
	//ASSERT(4 % null == div by zero)

	ASSERT(1 + 1 == 2)
	ASSERT(null + 1 == 1)
	ASSERT(1 + null == 1)

	ASSERT(1 - 1 == 0)
	ASSERT(null - 1 == -1)
	ASSERT(1 - null = 1)

	