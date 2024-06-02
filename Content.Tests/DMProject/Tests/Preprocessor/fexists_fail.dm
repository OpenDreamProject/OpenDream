// COMPILE ERROR
#if fexists("fake/path.abc")
#warn "it somehow exists"
#else
#error "it doesn't exist, yay"
#endif

/proc/RunTest()
	return