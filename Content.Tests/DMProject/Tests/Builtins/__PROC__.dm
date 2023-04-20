
/atom/movable/proc/foo()
	return __PROC__

/proc/bar()
	return __PROC__

/proc/RunTest()
	var/atom/movable/A = new
	ASSERT(A.foo() == /atom/movable/proc/foo)
	ASSERT(bar() == /proc/bar)
