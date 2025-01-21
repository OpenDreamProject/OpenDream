
//# issue 1653

/proc/RunTest()
	. = "foobar"
	spawn(0)
		ASSERT(. == "foobar")
