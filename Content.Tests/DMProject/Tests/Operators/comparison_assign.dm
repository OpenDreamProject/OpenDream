/proc/RunTest()
	var/datum/a = new
	var/datum/b = null

	ASSERT((b &&= a) == b)
	ASSERT(b == null)

	ASSERT((b ||= a) == a)
	ASSERT(b == a)