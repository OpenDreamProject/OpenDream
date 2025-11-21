/datum/foo
	var/thing = 2

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
		baz = list() //doesn't find the list()

	var foo = 2 //collects both of these at once, doesn't find the 2
	var bar

	Del()

/datum/foo/var
	datum/a
	datum/b

/proc/RunTest()
	var/datum/foo/F = new
	ASSERT(F.active == 0)
	ASSERT(F.deleting == 0)
	ASSERT(isnull(F.full_delete))
	ASSERT(F.delay == 5)
	ASSERT(islist(F.connected))
	ASSERT(!length(F.connected))
	ASSERT(isnull(F.height))
