
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

//Overrides on global procs should prefer the topmost, /proc/ version, regardless of ordering.
/proc/glob_say_definition()
	return "good"
/glob_say_definition()
	return "bad"

/you_cant_override_these()
	return "bad"
/proc/you_cant_override_these()
	return "good"

/proc/RunTest()
	var/d = new /datum/say_the_definition()
	ASSERT(d:f() == "good")
	d = new /datum/say_the_override()
	ASSERT(d:f() == "good")
	d = new /datum/composite_the_two()
	ASSERT(d:f() == "good")
	//
	ASSERT(glob_say_definition() == "good")
	ASSERT(you_cant_override_these() == "good")
