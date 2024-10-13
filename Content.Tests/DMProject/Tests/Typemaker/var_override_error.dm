// COMPILE ERROR OD2702
#pragma InvalidVarType error
/datum/do/re/mi/fa/so
	meep = "foo"

/datum/do
	var/meep = 5 as num

/proc/RunTest()
	return
