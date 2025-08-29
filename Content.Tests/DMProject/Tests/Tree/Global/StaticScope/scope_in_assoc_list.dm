
/datum
	var/static/v = 5
	var/static/list/l = list("a" = v)

/proc/RunTest()
	var/datum/o = new
	ASSERT(o.l["a"] == 5)
