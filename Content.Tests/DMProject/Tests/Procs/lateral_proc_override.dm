
//Issue OD#933: https://github.com/OpenDreamProject/OpenDream/issues/933

/datum/say_the_definition/f()
	return "bad"
/datum/say_the_definition/proc/f()
	return "good"

/datum/say_the_override/proc/f()
	return "bad"
/datum/say_the_override/f()
	return "good"

/datum/composite_the_two/proc/f()
	return "go"
/datum/composite_the_two/f()
	return ..() + "od"

/proc/RunTest()
	var/d = new /datum/say_the_definition()
	ASSERT(d:f() == "good")
	d = new /datum/say_the_override()
	ASSERT(d:f() == "good")
	d = new /datum/composite_the_two()
	ASSERT(d:f() == "good")
