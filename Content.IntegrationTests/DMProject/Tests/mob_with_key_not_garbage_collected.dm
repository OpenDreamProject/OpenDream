//# issue 2704
// Mobs with keys are never garbage collected by BYOND

var/global/gc_no_key_deleted = 0
var/global/gc_with_key_deleted = 0

/mob/gc_test/key/New()
	key = "test"
	. = ..()

/mob/gc_test/key/Del()
	global.gc_with_key_deleted = 1
	. = ..()

/mob/gc_test/keyless/Del()
	global.gc_no_key_deleted = 1
	. = ..()

/proc/gc_test_mobkey_make_and_drop()
	var/mob/gc_test/key/K = new()
	var/mob/gc_test/keyless/KL = new()

/datum/unit_test/test_mob_with_key_not_garbage_collected/RunTest()
	gc_test_mobkey_make_and_drop()

	// If the mob doesn't have a key, it cleanly deletes with no refs.
	ASSERT(global.gc_no_key_deleted == 1)
	// a mob with a key doesn't get deleted
	ASSERT(global.gc_with_key_deleted == 0)

	// But an explicit del() still deletes it
	var/mob/gc_test/key/M = new()
	del(M)
	ASSERT(global.gc_with_key_deleted == 1)
