/datum/test
	var/normal_var = 123
	var/static/static_var = 321
/proc/RunTest()
	var/datum/test/t = new()
	var/varlist = t.vars
	ASSERT(length(varlist) == 6) // normal_var, static_var, tag, type, parent_type, vars
	ASSERT(varlist["static_var"] == 321)
	ASSERT(varlist["normal_var"] == 123)
	ASSERT(varlist["type"] == /datum/test)
	ASSERT(varlist["parent_type"] == /datum)
	ASSERT(("vars" in varlist))
	ASSERT(varlist["vars"] == varlist)