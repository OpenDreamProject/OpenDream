#pragma InvalidReturnType error
/datum/var/bar = "foobar" as text

/datum/proc/meep() as text
	var/datum/D = new /datum as /datum
	return D.bar

/proc/RunTest()
	return
