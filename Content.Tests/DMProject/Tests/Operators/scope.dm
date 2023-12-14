var/static/one = "one"

/datum
	//var/static/three = "static three"
	var/three = "three"

/proc/return_two()
	return "two"

/proc/RunTest()
	ASSERT(::one == "one")
	ASSERT(::return_two() == "two")
    
	var/datum/test
	ASSERT(test::three == "static three")
	ASSERT(/datum::three == "static three")
	ASSERT(test::three != "three")
	ASSERT(/datum::three != "three")
