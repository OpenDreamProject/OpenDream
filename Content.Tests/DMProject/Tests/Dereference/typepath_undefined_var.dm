// Reading a var off a type path gives its initial value, or null if the type doesn't have it
/datum/typepath_has_var
	var/flag = 5
/datum/typepath_no_var

/proc/RunTest()
	var/has = /datum/typepath_has_var
	var/none = /datum/typepath_no_var
	ASSERT(has:flag == 5)
	ASSERT(isnull(none:flag))
