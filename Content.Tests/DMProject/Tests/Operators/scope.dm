var/global/thing = "thing"

/datum/var/static/other_thing = "other thing"

/proc/return_thing()
	return "thing"

/proc/RunTest()
	ASSERT(::thing == "thing")
	ASSERT(::return_thing() == "thing")
	
	var/datum/test
	ASSERT(test::other_thing == "other thing")
	//ASSERT(/datum::other_thing == "other thing")
