var/const/lib = "../../../byondapitest.dll"

/obj/testItem
	var/a = 123
	var/b = "asdf"

/proc/RunTest()
	var/obj/testItem/foo = new()
	var/obj/testItem/bar = new()
	var/result = call_ext(lib, "byond:byondapitest_equals")(1,1)
	ASSERT(result == (1==1))

	result = call_ext(lib, "byond:byondapitest_equals")(1,2)
	ASSERT(result == (1==2))

	result = call_ext(lib, "byond:byondapitest_equals")("a","a")
	ASSERT(result == ("a"=="a"))

	result = call_ext(lib, "byond:byondapitest_equals")("a","b")
	ASSERT(result == ("a"=="b"))

	result = call_ext(lib, "byond:byondapitest_equals")(list(1,1),list(1,1))
	ASSERT(result == (list(1,1)==list(1,1)))

	result = call_ext(lib, "byond:byondapitest_equals")(list(1,2),list(1,2))
	ASSERT(result == (list(1,2)==list(1,2)))

	result = call_ext(lib, "byond:byondapitest_equals")(foo,bar)
	ASSERT(result == (foo==bar))

	bar.b = "asd"
	result = call_ext(lib, "byond:byondapitest_equals")(foo,bar)
	ASSERT(result == (foo==bar))