var/static/one = "one"

/datum
	var/static/three = "static three"
	var/static/datum/four/four
	//var/three = "three"
	
/datum/four
	var/static/datum/five/five
	
/datum/five
	var/static/six = "static four five six"

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
	
	ASSERT(/datum::four::five::six == "static four five six")
