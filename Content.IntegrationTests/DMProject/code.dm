//Some setup code stuff
/turf
	var/isBlue = FALSE

/turf/blue
	isBlue = TRUE

//The actual tests
//NOTE: Tests placed in the IntegrationTests suite
// should actually require a normal server in order to work.
// Tests that only check generic DM behaviour that is not icon nor map-facing
// should be done within Content.Tests.
// ALSO NOTE: You do need to rebuild even if you just edit this document. Don't ask me why.

//Tests that range is iterating along the correct, wonky path
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
		CRASH("range(1,centre) iterated over [i - 1] tiles, expected 10")
	//Test that arguments are parsed correctly
	var/std = range(1,centre)
	if(std ~! range(centre,1))
		CRASH("range(1,centre) and range(centre,1) do not return the same result.")
	if(std ~! range("3x3",centre))
		CRASH("ViewRange argument parsing for range() isn't working correctly.")

/world/New()
	..()
	test_range()
	world.log << "Tests finished!"