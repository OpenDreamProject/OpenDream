/datum/a
/datum/b

/proc/RunTest()
	var/datum/a/A = new()
	var/datum/b/B = new()

	// This currently fails when the test isn't run in isolation
	// TODO: Fix whatever is persisting between tests that is making this fail
	/*var/count = 0
	for (var/datum/D)
		world.log << D.type
		count++
	ASSERT(count == 2)*/

	for (var/datum/a/D)
		ASSERT(istype(D, /datum/a))

	for (var/datum/b/D)
		ASSERT(istype(D, /datum/b))
