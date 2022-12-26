
// opendream_procpath is equivalent to ..... in BYOND

/atom/movable/proc/foo()
	return opendream_procpath

/proc/bar()
	return opendream_procpath

/proc/RunTest()
	var/atom/movable/A = new
	ASSERT(A.foo() == "/atom/movable/proc/foo")
	ASSERT(bar() == "/proc/bar")
