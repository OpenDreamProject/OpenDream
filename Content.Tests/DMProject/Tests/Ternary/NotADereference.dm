/proc/RunTest()
	var/teststring = "test"
	var/testchar = "w"

	ASSERT((teststring[1]==testchar?teststring[2]:teststring[1]) == "t")
