
/obj
	var/thing[]

/proc/RunTest()
	var/obj/o = new
	ASSERT(isnull(o.thing))
