// PR #2390

/proc/RunTest(var/datum/item)
	// Normally you can't reference a local var in a static var
	// But the const-fold here ends up not emitting an error for item.type
	var/static/static_var = "type" || item.type