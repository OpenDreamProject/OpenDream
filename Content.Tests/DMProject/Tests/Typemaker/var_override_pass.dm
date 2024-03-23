#pragma InvalidVarType error
/datum/do/re/mi/fa/so
	meep = null

/datum/do
	var/meep = 5 as num|null

/proc/RunTest()
	return
