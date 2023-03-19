
/datum/do/re/mi/fa/so/f()
	return ..() + " so"

/datum/do/re/f()
	return ..() + " re"

/datum/do/re/mi/fa/f()
	return ..() + " fa"

/datum/do/re/mi/f()
	return ..() + " mi"

/datum/do/re/mi/fa/so/la/f()
	return ..() + " la"

/datum/do/re/mi/fa/so/la/ti/do/f()
	return ..() + " ti do!"

/datum/do/proc/f()
	return "do"

/proc/RunTest()
	var/d = new /datum/do/re/mi/fa/so/la/ti/do()
	ASSERT(d:f() == "do re mi fa so la ti do!")
