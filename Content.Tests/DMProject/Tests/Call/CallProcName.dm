/datum/proc/call_target()
	return 13

/proc/RunTest()
	var/datum/target = new()

	ASSERT(call(target, "call_target")() == 13)