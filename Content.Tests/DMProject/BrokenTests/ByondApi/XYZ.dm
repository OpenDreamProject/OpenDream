var/const/lib = "../../../byondapitest.dll"

/mob/test_mob
	name = "test"

/proc/RunTest()
	var/mob/test_mob/test = new()
	test.loc=locate(1,2,1)
	var/result = call_ext(lib, "byond:byondapitest_xyz")(test,1,2,1)
	ASSERT(result == 0)

	test.loc=locate(10,10,1)
	result = call_ext(lib, "byond:byondapitest_xyz")(test,10,10,1)
	ASSERT(result == 0)