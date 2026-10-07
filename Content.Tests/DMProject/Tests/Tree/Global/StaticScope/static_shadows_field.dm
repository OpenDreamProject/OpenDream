// A proc's static var shadows an object var of the same name
/datum/handler
	var/list/L = list("a", "b", "c")

/datum/handler/proc/do_thing()
	var/static/list/L = list("1", "2", "3")
	return L[1]

/proc/RunTest()
	var/datum/handler/H = new()
	ASSERT(H.do_thing() == "1")
