/proc/RunTest()
	ASSERT("abc"[1] == "a")
	ASSERT("abc"[1.9] == "a")
	ASSERT("abc"[TRUE] == "a")

	ASSERT("é"[1] == "é")
