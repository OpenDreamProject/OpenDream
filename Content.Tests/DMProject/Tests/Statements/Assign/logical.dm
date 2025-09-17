
//# issue 612

/datum
	var/a = 1

var/datum/o

/proc/RunTest()
	o ||= new()
	ASSERT(o.a)
