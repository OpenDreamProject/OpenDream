
/datum
	proc/TheCall()
		return 7

var/datum/i = new

/proc/RunTest()
	ASSERT(global.i.TheCall() == 7)
