/datum/ListNullObj1
	var/a[]
	var/b[5]

/proc/RunTest()
	var/datum/ListNullObj1/o = new
	ASSERT(!islist(o.a))
	ASSERT(islist(o.b))
	ASSERT(o.b.len == 5)
