/datum/test_holder
	var/test1
	var/test2
	var/test3
	var/test4

/proc/RunTest()
	var/savefile/F = new("savtest.sav")
	F["savtest1"] = "beep"
	F["savtest2"] = "boop"
	F["savtest3"] = "berp"
	F["savtest4"] = "borp"
	del(F)
	fcopy("savtest.sav", "savtest2.sav")
	var/savefile/F2 = new("savtest2.sav")
	var/datum/test_holder/T = new()
	T.test1 = F2["savtest1"]
	T.test2 = F2["savtest2"]

	F2["savtest1"] >> T.test1
	F2["savtest2"] >> T.test2
	F2["savtest3"] >> T.test3
	F2["savtest4"] >> T.test4

	ASSERT(T.test1 == "beep")
	ASSERT(T.test2 == "boop")
	ASSERT(T.test3 == "berp")
	ASSERT(T.test4 == "borp")
	fdel("savtest.sav")
	fdel("savtest2.sav")