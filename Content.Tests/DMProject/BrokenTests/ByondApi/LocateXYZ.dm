var/const/lib = "../../../byondapitest.dll"

/proc/RunTest()
	var/pos = list(1,1,1)
	var/result = call_ext(lib, "byond:byondapitest_locatexyz")(pos[1],pos[2],pos[3])
	ASSERT(result == locate(pos[1],pos[2],pos[3]))

	pos = list(100,100,1)
	result = call_ext(lib, "byond:byondapitest_locatexyz")(pos[1],pos[2],pos[3])
	ASSERT(result == locate(pos[1],pos[2],pos[3]))

	pos = list(2,2,1)
	result = call_ext(lib, "byond:byondapitest_locatexyz")(pos[1],pos[2],pos[3])
	ASSERT(result == locate(pos[1],pos[2],pos[3]))

	pos = list(100,100,100)
	result = call_ext(lib, "byond:byondapitest_locatexyz")(pos[1],pos[2],pos[3])
	ASSERT(result == locate(pos[1],pos[2],pos[3]))
