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

//Tests that /proc/range() is iterating along the correct, wonky path
/proc/test_range()
	//Test that it goes in the right order
	var/list/correctCoordinates = list(
		list(3,3),
		list(2,2),
		list(2,3),
		list(2,4),
		list(3,2),
		list(3,4),
		list(4,2),
		list(4,3),
		list(4,4)
	)
	var/i = 1
	var/turf/centre = locate(3,3,1)
	for(var/x in range(1,centre))
		var/turf/T = x
		ASSERT(!isnull(T))
		var/list/coords = correctCoordinates[i]
		ASSERT(coords[1] == T.x)
		ASSERT(coords[2] == T.y)
		i += 1
	if(i != 10)
		CRASH("range(1,centre) iterated over [i - 1] tiles, expected 9")
	//Test that arguments are parsed correctly
	var/std = range(1,centre)
	if(std ~! range(centre,1))
		CRASH("range(1,centre) and range(centre,1) do not return the same result.")
	if(std ~! range("3x3",centre))
		CRASH("ViewRange argument parsing for range() isn't working correctly.")
	//Test that getting the range from a mob includes the mob's loc.
	var/list/mob_seen_turfs = list()
	var/mob/test/timmy = new(centre)
	for(var/turf/x in range(1,timmy))
		mob_seen_turfs += list(x)
	if(std ~! mob_seen_turfs)
		CRASH("Using a non-/turf Center for range() did not work correctly.")
	del(timmy)

/proc/test_color_matrix()
	var/r = "#de000000"
	var/g = "#00ad0000"
	var/b = "#0000be00"
	var/a = "#000000ef" // deadbeef my beloved
	var/mob/M = new
	M.color = list(r,g,b,a)
	if(M.color != "#deadbe")
		CRASH("Color matrix transformation in rgba() value didn't work correctly, color is '[json_encode(M.color)]' instead.")

/world/New()
	..()
	test_world_init()
	test_range()
	test_color_matrix()
	world.log << "IntegrationTests successful, /world/New() exiting..."