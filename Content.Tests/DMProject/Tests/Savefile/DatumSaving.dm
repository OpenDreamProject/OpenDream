/datum/foo
	var/best_map = "pl_upward"		// mutated by RunTest(), should save
	var/worst_map = "pl_badwater"	// same as, should not be saved
	var/null_me = "ok"				// should save as null
	
	var/tmp/current_map = "yeah"   // tmp, should not save
	var/const/default_cube = "delete it" // const, should not save

	New(args)
		proc_call_order_check += list("New")
		..()

	Read(savefile/F)
		proc_call_order_check += list("Read")
		..()

	Write(savefile/F)
		proc_call_order_check += list("Write")
		..()

/var/static/proc_call_order_check = list()


/proc/RunTest()
	var/savefile/S = new()

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

	var/datum/foo/W 
	S["mapdata"] >> W
	
	// load test
	ASSERT(istype(W))
	ASSERT(W != F) //they are equivelant, but not the same datum
	ASSERT(W.best_map == "pl_pier")
	ASSERT(W.worst_map == "pl_badwater")
	ASSERT(W.null_me == null)
	
	ASSERT(proc_call_order_check ~= list("New","Write","New","Read"))
   