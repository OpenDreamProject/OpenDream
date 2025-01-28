
/proc/p1()
	ASSERT(a == 0)
	ASSERT(g1 == null)
	ASSERT(g2 == 5)
	/var/static/a = isnull(g2) ? 1 : 0
	ASSERT(a == 0)
	ASSERT(g1 == null)
	ASSERT(g2 == 5)
	return a

/var/static/g2 = 5
/var/static/g1 = p1()

/proc/RunTest()
	ASSERT(g1 == 0)
	ASSERT(g2 == 5)
