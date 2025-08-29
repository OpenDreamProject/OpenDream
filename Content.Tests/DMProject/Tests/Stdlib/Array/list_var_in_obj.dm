
/datum
	var/thing[]

/proc/RunTest()
	var/datum/o = new
	ASSERT(isnull(o.thing))
