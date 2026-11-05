// for(var/X as type) with no list iterates the world, filtered by the "as" types
/obj/for_as_test
/mob/for_as_test

/proc/RunTest()
	var/obj/for_as_test/O = new()
	var/mob/for_as_test/M = new()

	var/found_obj = FALSE
	for (var/A as obj)
		ASSERT(isobj(A))
		if (A == O)
			found_obj = TRUE
	ASSERT(found_obj)

	var/found_obj2 = FALSE
	var/found_mob = FALSE
	for (var/A as obj|mob)
		ASSERT(isobj(A) || ismob(A))
		if (A == O)
			found_obj2 = TRUE
		else if (A == M)
			found_mob = TRUE
	ASSERT(found_obj2 && found_mob)

	var/found_any = FALSE
	for (var/A)
		if (A == M)
			found_any = TRUE
	ASSERT(found_any)
