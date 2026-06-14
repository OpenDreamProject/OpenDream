
/datum
	var/a[]
	var/b[5]

/proc/RunTest()
	var/datum/o = new
	ASSERT(isnull(o.a))
	ASSERT(islist(o.b))
	ASSERT(o.b.len == 5)
