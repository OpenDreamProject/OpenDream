// #2178

/datum/foo
	var
		tmp
			active = 0
			deleting = 0
			full_delete
			delay = 5
		list
			connected = list()

		height = null

/proc/RunTest()
	var/datum/foo/F = new
	ASSERT(F.active == 0)
	ASSERT(F.deleting == 0)
	ASSERT(isnull(F.full_delete))
	ASSERT(F.delay == 5)
	ASSERT(!issaved(F.active))
	ASSERT(!issaved(F.deleting))
	ASSERT(!issaved(F.full_delete))
	ASSERT(!issaved(F.delay))
	ASSERT(islist(F.connected))
	ASSERT(!length(F.connected))
	ASSERT(isnull(F.height))