/datum/one
	var/text = "one"
	var/datum/two/two

/datum/two/var/text = "two"

/proc/RunTest()
	ASSERT(/datum/one::text == "one")
	ASSERT(/datum/one::two::text == "two")
