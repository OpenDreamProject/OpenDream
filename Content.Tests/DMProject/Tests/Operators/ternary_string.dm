
//# issue 416

/proc/RunTest()
	var/b = "b"
	ASSERT((1?"a":b + "c") == "a")
