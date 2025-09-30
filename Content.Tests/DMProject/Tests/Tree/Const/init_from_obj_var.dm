
/datum
	var/const/a = 5

var/datum/o = new

var/const/a = o.a

/proc/RunTest()
	ASSERT(a == 5)
