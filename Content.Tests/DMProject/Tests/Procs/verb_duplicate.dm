/mob/proc/test()
	return

/proc/RunTest()
	var/mob/m = new
	m.verbs += /mob/proc/test
	m.verbs += /mob/proc/test
	ASSERT(m.verbs.len == 1)
