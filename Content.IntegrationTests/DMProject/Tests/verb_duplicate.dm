/mob/proc/test()
	return

/proc/test_verb_duplicate()
	var/mob/m = new
	m.verbs += /mob/proc/test
	m.verbs += /mob/proc/test
	ASSERT(m.verbs.len == 1)
