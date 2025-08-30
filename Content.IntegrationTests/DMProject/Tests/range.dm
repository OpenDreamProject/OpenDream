//Tests that /proc/range() is iterating along the correct, wonky path
/datum/unit_test/range/RunTest()
	world.maxx = world.maxy = 5
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