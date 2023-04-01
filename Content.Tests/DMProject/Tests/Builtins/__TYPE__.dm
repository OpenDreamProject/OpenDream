
/atom/movable/proc/foo()
	return __TYPE__

/proc/bar()
	return __TYPE__

/proc/RunTest()
	var/atom/movable/A = new
	ASSERT(A.foo() == /atom/movable)
	ASSERT(isnull(bar()))
