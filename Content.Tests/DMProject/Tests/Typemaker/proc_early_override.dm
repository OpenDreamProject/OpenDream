// COMPILE ERROR
#pragma InvalidReturnType error

/datum/do/re/mi/fa/so/f()
	return 5

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

/datum/do/proc/f() as text
	return "do"

/proc/RunTest()
	return
