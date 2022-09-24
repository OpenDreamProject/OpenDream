/obj/ListNullObj2
	var/a[]
	var/b[5][3]

/proc/RunTest()
	var/obj/ListNullObj2/o = new
	ASSERT(!islist(o.a))
	ASSERT(islist(o.b))
	ASSERT(islist(o.b[1]))
	ASSERT(o.b[1].len == 3)
