/datum/listhaver
	var/list/C[][] = list()
	var/D[] = new()

/proc/RunTest()
	var/a[]
	var/b[5]
	var/datum/listhaver/LH = new /datum/listhaver()
	ASSERT(islist(LH.C))
	ASSERT(islist(LH.D))

	ASSERT(!islist(a))
	ASSERT(islist(b))
	ASSERT(b.len == 5)
