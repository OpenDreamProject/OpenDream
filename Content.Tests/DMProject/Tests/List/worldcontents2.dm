/proc/RunTest()
	var/obj/object2 = null
	for(var/i in 1 to 3)
		var/obj/O = new /obj(locate(1,1,1))
		O.name = "object [i]"
		if(i==2)
			object2 = O
	var/startcount  = length(world.contents)
	var/i = 0
	for(var/O in world)
		if(i==3)
			del(object2)
			ASSERT(length(world.contents) == startcount-1)
		i++
	ASSERT(i == startcount)