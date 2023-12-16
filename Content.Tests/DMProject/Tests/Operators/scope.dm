var/static/one = "one"

/datum
	var/static/datum/three/three

/datum/three
	var/static/datum/four/four

/datum/four
	var/datum/five/five

/datum/five
	var/static/six = "three four five six"

/proc/return_two()
	return "two"

/proc/RunTest()
	// global vars and procs
	ASSERT(::one == "one")
	ASSERT(::return_two() == "two")

	// static vars + chaining
	var/datum/test
	ASSERT(test::three::four.five::six == "three four five six")
	ASSERT(/datum::three::four.five::six == "three four five six")

	// reassigning global and static vars
	::one = "1"
	ASSERT(::one == "1")

	/datum::three::four.five::six = "3 4 5 6"
	ASSERT(test::three::four.five::six == "3 4 5 6")
	ASSERT(/datum::three::four.five::six == "3 4 5 6")

	test::three::four.five::six = "7 8 9 10"
	ASSERT(test::three::four.five::six == "7 8 9 10")
	ASSERT(/datum::three::four.five::six == "7 8 9 10")
