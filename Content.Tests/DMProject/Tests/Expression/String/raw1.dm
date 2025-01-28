//# issue 380

/proc/RunTest()
	var/a = @{"
asdf"}
	ASSERT(a == "asdf")