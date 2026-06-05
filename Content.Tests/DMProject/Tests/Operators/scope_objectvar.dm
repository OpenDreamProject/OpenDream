// #2610

/datum/a
	var/const/constant = 10

var/datum/a/global_var = /datum/a

/datum/b
	var/variable = global_var::constant

/proc/RunTest()
	var/datum/b/B = new()
	ASSERT(B.variable == 10)