/obj/savetest

/proc/RunTest()
	var/obj/savetest/O = new() //create a test object
	O.name = "test"

	var/savefile/F = new() //create a temporary savefile

	F["dir"] = O
	F["dir2"] = "object(\".0\")"
	F["dir3"] = 1080
	F["dir4"] = "the afternoon of the 3rd"
	F["dir6/subdir6"] = 321
	F["dir7"] = null
	F["dir8"] = "double entry 1 = 5; double entry 2 = 10"
	F["list"] << list("1",2,"three"=3,4, new /datum(), new /datum(), list(1,2,3, new /datum()))

	var/total_export = F.ExportText()

	var/savefile/F2 = new()
	F2.ImportText("/",total_export)
	//object test
	ASSERT(F2["dir"].name == "test")
	//string test
	ASSERT(F2["dir2"] == F["dir2"])
	ASSERT(F2["dir4"] == F["dir4"])
	//int test
	ASSERT(F2["dir3"] == F["dir3"])
	//null test
	ASSERT(F2["dir7"] == null)
	//subdir test
	ASSERT(F2["dir6"] == null)
	ASSERT(F2["dir6/subdir6"] == 321)
	//list test
	ASSERT(F2["list"][1] == "1")
	ASSERT(F2["list"][2] == 2)
	ASSERT(F2["list"]["three"] == 3)
	ASSERT(F2["list"][4] == 4)
	ASSERT(istype(F2["list"][5], /datum))
	ASSERT(istype(F2["list"][6], /datum))
	//nested list
	ASSERT(F2["list"][7][1] == 1)
	ASSERT(F2["list"][7][2] == 2)
	ASSERT(F2["list"][7][3] == 3)
	ASSERT(istype(F2["list"][7][4], /datum))
	//multiple entry test
	ASSERT(F2["dir8"]["double entry 1"] == 5)
	ASSERT(F2["dir8"]["double entry 2"] == 10)