// #2610

/datum/a
	var/const/constant = 10

var/datum/a/global_var = /datum/a

/datum/b
	var/variable = global_var::constant
	var/variable2 = global_var::constant / 5

/proc/RunTest()
	var/datum/b/B = new()
	ASSERT(B.variable == 10)
	ASSERT(B.variable2 == 2)