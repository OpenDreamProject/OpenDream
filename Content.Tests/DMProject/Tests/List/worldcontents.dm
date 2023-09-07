/obj/one
	name = "one"
/obj/two
	name = "two"


/proc/RunTest()
	var/obj/onehandle = new /obj/one(locate(1,1,1))
	ASSERT(length(world.contents) == 1)
	var/count = 0
	for(var/obj/O in world)
		count++	
	ASSERT(count == 1)

	for(var/obj/O in world)
		count++
		new /obj/two(locate(1,1,1))
		ASSERT(length(world.contents) == 1) //while iterating over world.contents, adding a new object should be buffered still
	ASSERT(count == 1)
	ASSERT(length(world.contents) == 2) //it should no longer be buffered
	
	count = 0
	var/worldcontlen = length(world.contents)
	for(var/obj/O in world)
		count++
		world.contents -= onehandle
		ASSERT(worldcontlen == length(world.contents)) //removing an object should be buffered
	ASSERT(worldcontlen == length(world.contents)+1) //it should no longer be buffered

