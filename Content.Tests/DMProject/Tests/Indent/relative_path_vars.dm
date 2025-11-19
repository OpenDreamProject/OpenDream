/datum/foo
	var
		tmp
			active = 0
			deleting = 0
			full_delete = 0
			delay = 5
		list
			connected = list()

/proc/RunTest()
	var/datum/foo/F = new
	ASSERT(F.active == 0)
	ASSERT(F.deleting == 0)
	ASSERT(F.full_delete == 0)
	ASSERT(F.delay == 5)
	ASSERT(islist(F.connected))
	ASSERT(!length(F.connected))