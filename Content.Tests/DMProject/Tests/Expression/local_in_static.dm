// COMPILE ERROR OD0011
// PR #2390

/proc/RunTest()
	var/datum/item
	
	// Can't use a local var in a static var
	var/static/static_var = item.type