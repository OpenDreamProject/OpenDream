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

	proc/thing()

	var
		baz = list()

	var foo = 2
	var bar

	Del()

/proc/RunTest()
	var/datum/foo/F = new
	ASSERT(F.active == 0)
	ASSERT(F.deleting == 0)
	ASSERT(isnull(F.full_delete))
	ASSERT(F.delay == 5)
	ASSERT(islist(F.connected))
	ASSERT(!length(F.connected))
	ASSERT(isnull(F.height))