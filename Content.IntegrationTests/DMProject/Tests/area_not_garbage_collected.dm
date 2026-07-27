//# issue 2641
// Areas are reference counted, but BYOND never garbage collects them

var/global/gc_area_deleted = 0
var/global/gc_datum_deleted = 0

/area/gc_test/Del()
	global.gc_area_deleted = 1
	..()

/datum/gc_test/Del()
	global.gc_datum_deleted = 1
	..()

// Both objects are unreferenced once this returns
/proc/gc_test_make_and_drop()
	var/area/gc_test/A = new()
	var/datum/gc_test/D = new()

/datum/unit_test/test_area_not_garbage_collected/RunTest()
	gc_test_make_and_drop()

	// A plain datum dies as soon as nothing references it anymore...
	ASSERT(global.gc_datum_deleted == 1)
	// ...an area does not
	ASSERT(global.gc_area_deleted == 0)

	// But an explicit del() still deletes it
	var/area/gc_test/B = new()
	del(B)
	ASSERT(global.gc_area_deleted == 1)
