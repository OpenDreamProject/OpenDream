/proc/RunTest()
    for(var/j in 1 to 3)
        var/obj/O = new /obj(locate(1,1,1))
        O.name = "object [j]"
    var/startcount  = length(world.contents)
    var/i = 0
    for(var/O in world)
        if(i==1)
            new /obj(locate(1,1,1))
            ASSERT(length(world.contents) == startcount+1)
        i++
    ASSERT(i == startcount+1)