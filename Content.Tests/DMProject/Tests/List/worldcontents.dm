/obj/one
	name = "one"
/obj/two
	name = "two"

/proc/RunTest()
	var/start_world_count = length(world.contents)
	var/obj/onehandle = new /obj/one(locate(1,1,1))
	onehandle.name = "one \ref[onehandle]"

	ASSERT(length(world.contents) == start_world_count + 1 && "sanity check")
	var/count = 0
	for(var/obj/O in world)
		count++	
	count = 0		
	for(var/obj/O in world)
		count++	
	
	ASSERT(count == start_world_count + 1 && "more than one object in world")
	ASSERT(length(world.contents) == start_world_count + 1 && "iterating over world editted world.contents")

	count = 0
	for(var/obj/O in world)
		count++
		new /obj/two(locate(1,1,1))
		ASSERT(length(world.contents) == start_world_count + 1 && "length changed during buffered add") //while iterating over world.contents, adding a new object should be buffered still
	ASSERT(count == 1 && "more than one object in world during add")
	ASSERT(length(world.contents) == start_world_count + 2 && "length didn't change after buffered add") //it should no longer be buffered
	
	count = 0
	var/worldcontlen = length(world.contents)
	for(var/obj/O in world)
		count++
		del(onehandle)
		ASSERT(worldcontlen == length(world.contents) && "length changed during buffered remove") //removing an object should be buffered
	ASSERT(worldcontlen == length(world.contents)+1 && "length didn't change after buffered remove") //it should no longer be buffered

