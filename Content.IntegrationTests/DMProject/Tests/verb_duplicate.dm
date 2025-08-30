/mob/proc/test()
	return

/datum/unit_test/verb_duplicate/RunTest()
	var/mob/m = new
	m.verbs += /mob/proc/test
	m.verbs += /mob/proc/test
	ASSERT(m.verbs.len == 1)
