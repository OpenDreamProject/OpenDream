var/const/lib = "../../../byondapitest.dll"

/obj/test
	var/content = "success"

/proc/RunTest()
	var/obj/test/foo = new()
	var/key = "content"
	var/result = call_ext(lib, "byond:byondapitest_readvarbystrid_existent_var")(foo,key)
	ASSERT(result == 0)
	key = "nonexistent_content"
	result = call_ext(lib, "byond:byondapitest_readvarbystrid_nonexistent_var")(foo,key)
	ASSERT(result == 0)