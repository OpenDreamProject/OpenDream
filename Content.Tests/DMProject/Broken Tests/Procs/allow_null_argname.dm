
// TODO: BYOND allows var/null, OpenDream does not. Revisit when errors can be selectively disabled by pragmas

/proc/nullproc(null, a, b)
	ASSERT(null == 4)
	ASSERT(a == 5)
	ASSERT(b == 6)

/proc/RunTest()
	nullproc(4,5,6)
