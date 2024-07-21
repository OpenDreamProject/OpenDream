#define SOMEDEF1 1

/proc/RunTest()
	var/a = 1

	if(0)
		. = . // NOOP
	#if SOMEDEF1
	a = 2
	#endif
	ASSERT(a == 2)
