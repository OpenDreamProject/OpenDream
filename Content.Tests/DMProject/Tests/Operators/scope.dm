var/global/thing = "thing"

/proc/return_thing()
	return "thing"

/proc/RunTest()
	ASSERT(::thing == "thing")
	ASSERT(::return_thing() == "thing")
