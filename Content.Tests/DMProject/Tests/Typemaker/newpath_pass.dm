#pragma InvalidVarType error
/proc/foo(obj/bar as obj)
	bar = new /obj(null)

/proc/RunTest()
	return