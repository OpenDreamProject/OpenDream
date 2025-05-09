var/const/lib = "../../../byondapitest.dll"

obj/foo
	var/datum/bar = 1

/proc/RunTest()
	var/val = "true"
	var/result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	val="false"
	result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	val=""
	result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	val=-1
	result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	val=0
	result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	val=1
	result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	val=null
	result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	val=list()
	result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	val=list(0)
	result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	val=list(1)
	result = call_ext(lib, "byond:byondapitest_istrue")(val)
	ASSERT((result?1:0) ^ (val?1:0) == 0)

	var/obj/foo/foobar= new()
	result = call_ext(lib, "byond:byondapitest_istrue")(foobar)
	ASSERT((result?1:0) ^ (foobar?1:0) == 0)
