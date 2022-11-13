
//# issue 534

/proc/ffn(path)
	return path

/atom/proc/fn(a,b)
	ASSERT(0)

/proc/RunTest()
	ASSERT(ffn(/atom./proc/fn) == /atom/proc/fn)
