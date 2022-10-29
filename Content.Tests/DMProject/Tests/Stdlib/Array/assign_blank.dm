// RUNTIME ERROR

/obj
	var/thing[]

/proc/RunTest()
	var/obj/o = new
	ASSERT(!isnull(o.thing))
	o.thing[3] = 5 // This is where the runtime occurs
