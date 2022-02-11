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

	verb/shake()
		animate(src, pixel_x = -4, time = 2)
		sleep(2)
		for (var/i in 1 to 3)
			animate(src, pixel_x = 4, time = 4)
			sleep(4)
			animate(src, pixel_x = -4, time = 4)
			sleep(4)
		animate(src, pixel_x = 0, time = 2)

	verb/tell_location()
		usr << "You are at ([x], [y], [z])"

	verb/say(message as text)
		var/list/viewers = viewers()

		for (var/mob/viewer in viewers)
			viewer << "[ckey] says: \"[message]\""

	verb/say_loud()
		var/msg = input("Please put the message you want to say loudly.", "Say Loud", "Hello!")
		world << "[ckey] says loudly: \"[msg]\""

	verb/move_up()
		step(src, UP)

	verb/move_down()
		step(src, DOWN)

/obj/a
	name = "a"

/obj/a/b/c/d/e/f/g/h/i/j/k/l/m/n/o/p
	name = "p"

/world/New()
	..()
	world.log << "World loaded!"

	var/obj/a/ancestor = new()
	var/obj/a/b/c/d/e/f/g/h/i/j/k/l/m/n/o/longancestor = new()
	var/obj/a/b/c/d/e/f/g/h/i/j/k/l/m/n/o/p/longchild = new()

	var/t1
	var/const/loops = 4000000

	var/time1 = world.timeofday
	for(var/i in 1 to loops)
		istypetest(longchild, ancestor, longancestor)
	t1 = world.timeofday-time1
	world.log << "TOTAL TIME FOR [loops] LOOPS: [t1]"

/proc/istypetest(longchild, ancestor, longancestor)
	var/x = istype(longchild, ancestor)
	var/y = istype(longchild, longancestor)
