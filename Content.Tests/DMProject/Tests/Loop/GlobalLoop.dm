/datum/a
/datum/b

/proc/RunTest()
	var/datum/a/A = new()
	var/datum/b/B = new()

	var/count = 0
	for (var/datum/D)
		count++
	ASSERT(count == 2)

	for (var/datum/a/D)
		ASSERT(istype(D, /datum/a))

	for (var/datum/b/D)
		ASSERT(istype(D, /datum/b))
