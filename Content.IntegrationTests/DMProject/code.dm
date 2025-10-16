//Setup code - put whatever you need for testing here.
/turf
/turf/border
/mob/test

//The actual tests
//NOTE: Tests placed in the IntegrationTests suite
// should actually require a normal server in order to work.
// Tests that only check generic DM behaviour that is not icon nor map-facing
// should be done within Content.Tests.
// ALSO NOTE: If you're getting strange results, try rebuilding to make sure this DM file is copied correctly.

//Basic sanity check that the map actually loads correctly.
/proc/test_world_init()
	var/a = 0
	var/b = 0
	for(var/turf/t in world)
		if(istype(t,/turf/border))
			b += 1
		else
			a += 1
	if(a + b != 75)
		CRASH("Map probably failed to load; expected 75 tiles in the map, instead found [a + b].")

/datum/unit_test/proc/RunTest()
	throw EXCEPTION("You must override RunTest()")

/world/New()
	for(var/subtype in typesof(/datum/unit_test))
		if(subtype == /datum/unit_test) //skip the base class
			continue
		var/datum/unit_test/TEST = new subtype()
		TEST.RunTest()
		del(TEST) //and clean up
	world.log << "IntegrationTests successful, /world/New() exiting..."
	spawn(10)
		Del(world)