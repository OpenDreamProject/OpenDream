
/proc/_initial(...)
	ASSERT(initial(arglist(args))[1] == "foo")

/proc/RunTest()
	_initial("foo")
