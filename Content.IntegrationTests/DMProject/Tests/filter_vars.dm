// Writing a var of a filter from atom.filters changes the atom's filter
/datum/unit_test/filter_vars_write/RunTest()
	var/obj/O = new(locate(2,2,1))

	// Index, name and for references all write correctly, and every reference sees the result
	O.filters = list(filter(type="blur", size=1, name="foo"), filter(type="outline", size=1))
	var/dm_filter/F = O.filters[1]
	var/dm_filter/G = O.filters["foo"]
	F.size = 3
	ASSERT(O.filters[1]:size == 3)
	ASSERT(G.size == 3)
	G.size = 4
	ASSERT(F.size == 4)
	for(var/loop_filter in O.filters)
		loop_filter:size = 5
	ASSERT(O.filters[1]:size == 5)
	ASSERT(O.filters[2]:size == 5)

	// Adding a filter stores a copy, so later writes to the original don't affect the atom
	var/dm_filter/original = filter(type="blur", size=1)
	O.filters = original
	original.size = 3
	ASSERT(O.filters[1]:size == 1)

	// Removing an earlier filter doesn't detach a later one
	O.filters = list(filter(type="blur", size=1), filter(type="blur", size=2))
	F = O.filters[2]
	O.filters -= O.filters[1]
	F.size = 5
	ASSERT(O.filters[1]:size == 5)

	// Images
	var/image/I = new
	I.filters += filter(type="blur", size=1)
	F = I.filters[1]
	F.size = 3
	ASSERT(I.filters[1]:size == 3)

// Writing a var changes every identical filter on that atom, but not other atoms' filters
/datum/unit_test/filter_vars_identical/RunTest()
	var/obj/O = new(locate(2,2,1))
	var/obj/O2 = new(locate(2,2,1))
	var/dm_filter/shared = filter(type="blur", size=1)
	O.filters += shared
	O.filters += shared
	O2.filters += shared

	var/dm_filter/F = O.filters[2]
	F.size = 3
	ASSERT(O.filters[1]:size == 3)
	ASSERT(O2.filters[1]:size == 1)

// Writing a filter that was removed or replaced is a runtime error
/datum/unit_test/filter_vars_removed/RunTest()
	var/obj/O = new(locate(2,2,1))
	O.filters += filter(type="blur", size=1)
	var/dm_filter/removed = O.filters[1]
	O.filters = null
	O.filters += filter(type="blur", size=1)
	var/dm_filter/replaced = O.filters[1]
	O.filters[1] = filter(type="blur", size=2)

	var/failures = 0
	try
		removed.size = 3
	catch
		failures++
	try
		replaced.size = 3
	catch
		failures++
	ASSERT(failures == 2)
	ASSERT(O.filters[1]:size == 2)
