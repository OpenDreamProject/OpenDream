/datum/foo
	var/best_map = "pl_upward"		// mutated by RunTest(), should save
	var/worst_map = "pl_badwater"	// same as, should not be saved
	var/null_me = "ok"				// should save as null
	
	var/tmp/current_map = "yeah"   // tmp, should not save
	var/const/default_cube = "delete it" // const, should not save

/datum/foo/Write(savefile/F)
	. = ..(F)
	ASSERT(F["type"] == /datum/foo)
	ASSERT(F["current_map"] == null)
	ASSERT(F["default_cube"] == null)

/proc/RunTest()
	var/savefile/S = new("delme.sav")

	var/datum/foo/F = new()
	F.best_map = "pl_pier"
	F.null_me = null
	
	S["mapdata"] << F
	
	// test the savefile's contents
	ASSERT(S["mapdata/.0/type"] == /datum/foo)
	ASSERT(S["mapdata/.0/best_map"] == "pl_pier")
	ASSERT(S["mapdata/.0/null_me"] == null)
	ASSERT(S["mapdata/.0/worst_map"] == null)
	ASSERT(S["mapdata/.0/current_map"] == null)
	ASSERT(S["mapdata/.0/default_cube"] == null)

	var/datum/foo/W = new()
	S["mapdata"] >> W
	
	// load test
	ASSERT(istype(W))
	ASSERT(W.best_map == "pl_pier")
	ASSERT(W.worst_map == null)
	ASSERT(W.null_me == null)
	
	fdel("delme.sav")
	return TRUE
   