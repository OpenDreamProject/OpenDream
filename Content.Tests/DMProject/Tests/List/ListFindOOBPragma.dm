// NOBYOND
#pragma ListFindOutOfBoundsException error

/proc/RunTest()
	// BYOND ignores an OOB End param on some special lists
	var/obj/holder = new
	var/obj/held = new(holder)
	var/failed = FALSE
	try
		holder.contents.Find(held, 1, 99)
	catch
		failed = TRUE
	ASSERT(failed)

	// BYOND silently finds nothing with a negative Start
	var/list/L = list("a", "b", "c")
	failed = FALSE
	try
		L.Find("a", -1)
	catch
		failed = TRUE
	ASSERT(failed)
