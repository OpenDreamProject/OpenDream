var/const/lib = "../../../byondapitest.dll"

/obj/test
	var/content = "success"

/proc/RunTest()
	var/obj/test/foo = new()
	var/val = "success"
	var/result = call_ext(lib, "byond:byondapitest_writevarbystrid_existent_var")(foo,"content",val)
	ASSERT(result == 0)
	ASSERT(foo.content == "success")
	result = call_ext(lib, "byond:byondapitest_writevarbystrid_nonexistent_var")(foo,"nonexistent_content",val)
	ASSERT(result == 0)