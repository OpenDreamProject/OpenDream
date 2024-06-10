
/datum/foo

/proc/RunTest()
	var/datum/foo/F = new
	ASSERT(initial(F.parent_type) == /datum)
	
	var/datum/D = null
	ASSERT(isnull(initial(D.parent_type)))