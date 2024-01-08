/datum
	var/static/datum/three/three
	var/text = "hello"
	
var/static/one = "one"

/datum/three
	var/static/datum/four/four
	text = "hi"
	var/overridden_text = type::text
	var/original_text = parent_type::text
	
/datum/three/proc/typetest()
	// initial shorthand, type:: and parent_type::
	ASSERT(text == "hi")
	ASSERT(original_text == "hello")
	ASSERT(overridden_text == "hi")
	overridden_text = "hi there!"
	original_text = "hello there"
	text = "hi there"
	ASSERT(src::text == "hi")
	ASSERT(src::overridden_text == "hi")
	ASSERT(src::original_text == "hello")
	
	// proc reference
	ASSERT(__PROC__ == /datum/three::typetest())

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
	
	var/datum/three/threetest = new
	threetest.typetest()
