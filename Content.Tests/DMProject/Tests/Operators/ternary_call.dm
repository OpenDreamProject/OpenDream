
//# issue 705

/proc/a(v)
	return v

/proc/b(v)
	return v

/proc/RunTest()
	ASSERT((0 ? a(1):b(2)) == 2)
	ASSERT((1 ? a(1):b(2)) == 1)
