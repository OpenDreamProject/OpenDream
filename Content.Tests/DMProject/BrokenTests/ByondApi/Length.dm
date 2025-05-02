var/const/lib = "../../../byondapitest.dll"

/obj/foo
	var/bar = 123

/proc/RunTest()
	var/x = list()
	var/result = call_ext(lib, "byond:byondapitest_length")(x,length(x))
	ASSERT(result == 0)

	x = list(1,2)
	result = call_ext(lib, "byond:byondapitest_length")(x,length(x))
	ASSERT(result == 0)

	x = list(asdf=1,qwer=2)
	result = call_ext(lib, "byond:byondapitest_length")(x,length(x))
	ASSERT(result == 0)

	x = null
	result = call_ext(lib, "byond:byondapitest_length")(x,length(x))
	ASSERT(result == 0)

	x = ""
	result = call_ext(lib, "byond:byondapitest_length")(x,length(x))
	ASSERT(result == 0)

	x = "asdf"
	result = call_ext(lib, "byond:byondapitest_length")(x,length(x))
	ASSERT(result == 0)

	var/obj/foo/foobar = new()
	result = call_ext(lib, "byond:byondapitest_length")(foobar,length(foobar))
	ASSERT(result == 0)