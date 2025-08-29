/proc/test_change_world_size()
	var/oldz = world.maxz
	var/oldx = world.maxx
	var/oldy = world.maxy
	world.maxz = oldz+1
	ASSERT(world.maxz == oldz+1)
	world.maxx = oldx+1
	ASSERT(world.maxx == oldx+1)
	world.maxy = oldy+1
	ASSERT(world.maxy == oldy+1)
	ASSERT(istype(locate(oldx+1, oldy+1, oldz+1), /turf))

	world.maxz = oldz
	world.maxx = oldx
	world.maxy = oldy
	ASSERT(isnull(locate(oldx+1, oldy+1, oldz+1)))

