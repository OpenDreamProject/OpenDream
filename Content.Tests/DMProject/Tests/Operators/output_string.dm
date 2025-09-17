
//# issue 216

/datum/ty/New()
	fn()

/datum/ty/proc/fn(mob/user)
	user << "test"

/proc/RunTest()
	var/datum/ty/o = new

