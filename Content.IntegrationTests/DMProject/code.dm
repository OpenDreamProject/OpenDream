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
	if(a + b != 25)
		CRASH("Map probably failed to load; expected 25 tiles in the map, instead found [a + b].")


/world/New()
	..()
	test_world_init()
	test_block()
	test_color_matrix()
	test_range()
	test_verb_duplicate()
	world.log << "IntegrationTests successful, /world/New() exiting..."