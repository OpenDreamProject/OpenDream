
//# issue 693

#define subtypesof(T) typesof(T) - T
/obj
	var/static/list/C = subtypesof(/datum)

/proc/RunTest()
	var/obj/o = new
	// We don't care about the actual number, we just want it to work at all
	ASSERT(o.C.len > 1)
