
// TODO This test doesn't work in BYOND but I'm not sure why.
var/x = 1

/datum/var/x = 4
/atom/movable/var/static/h = (x * 2)

/proc/main()
	var/atom/movable/o = new
	var/datum/da = new
	world.log << (o.h)
	world.log << (da.x)
