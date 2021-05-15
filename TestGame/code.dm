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
	
	verb/md5_ckey()
	    var/hash = md5(ckey)
	    usr << "The md5 hash of your ckey is: [hash]"
	    
	verb/clamp_value()
	    var/out1 = clamp(10, 1, 5)
	    usr << "The output should be 5: [out1]"
	    var/out2 = clamp(-10, 1, 5)
	    usr << "The output should be 1: [out2]"

/world/New()
	..()
	world.log << "World loaded!"
