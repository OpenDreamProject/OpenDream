/turf
	icon = 'icons/turf.dmi'
	icon_state = "turf"

/turf/blue
	icon_state = "turf_blue"

/mob
	icon = 'icons/mob.dmi'
	icon_state = "mob"

	New()
		..()
		loc = locate(5, 5, 1)

	verb/tell_location()
		var/list/L = list(1, 2, 3)
		var/list/G = list(3, 4, 5)
		var/num = 6
		var/numm = 8

		world << json_encode(L | G)
		world << json_encode(G | L)
		//world << num | L
		world << json_encode(L | num)
		world << (num | numm)

/mob/Stat()
	statpanel("Status", "CPU: [world.cpu]")

/world/New()
	..()
	world.log << "World loaded!"
