/datum/test_holder
	var/test1
	var/test2
	var/test3
	var/test4

/proc/RunTest()
	var/savefile/F = new("test.sav")
	F["test1"] = "beep"
	F["test2"] = "boop"
	F["test3"] = "berp"
	F["test4"] = "borp"
	del(F)
	fcopy("test.sav", "test2.sav")
	var/savefile/F2 = new("test2.sav")
	var/datum/test_holder/T = new()
	F2["test1"] >> T.test1
	F2["test2"] >> T.test2
	F2["test3"] >> T.test3
	F2["test4"] >> T.test4

	ASSERT(T.test1 == "beep")
	ASSERT(T.test2 == "boop")
	ASSERT(T.test3 == "berp")
	ASSERT(T.test4 == "borp")
	fdel("test.sav")
	fdel("test2.sav")