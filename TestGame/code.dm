/turf
	icon = 'icons/turf.dmi'
	icon_state = "turf"

/mob
	icon = 'icons/mob.dmi'
	icon_state = "mob"

	New()
		..()
		loc = locate(5, 5, 1)

	Move(NewLoc, Dir)
		..()
		client << "You are now at (" + num2text(loc.x) + ", " + num2text(loc.y) + ")"

/world/New()
	..()
	world << "World loaded!"