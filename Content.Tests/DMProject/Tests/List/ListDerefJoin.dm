
//# issue 808

/proc/listtest()
	return list("a","b","c").Join()

/proc/RunTest()
	ASSERT(listtest() == "abc")
