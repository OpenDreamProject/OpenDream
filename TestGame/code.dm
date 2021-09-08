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

	verb/roll_dice(dice as text)
		var/result = roll(dice)
		usr << "The total shown on the dice is: [result]"

	verb/wordy()
		var/info = @{"Thank you for your purchase of the Nerd Co SpySpeks <small>tm</small>, this paper will be your quick-start guide to violating the privacy of your crewmates in three easy steps!<br><br>Step One: Nerd Co SpySpeks <small>tm</small> upon your face. <br>
        Step Two: Place the included "ProfitProtektor <small>tm</small>" camera assembly in a place of your choosing - make sure to make heavy use of it's inconspicous design!

		Step Three: Press the "Activate Remote View" Button on the side of your SpySpeks <small>tm</small> to open a movable camera display in the corner of your vision, it's just that easy!<br><br><br><center><b>TROUBLESHOOTING</b><br></center>
		My SpySpeks <small>tm</small> Make a shrill beep while attempting to use!

		A shrill beep coming from your SpySpeks means that they can't connect to the included ProfitProtektor <small>tm</small>, please make sure your ProfitProtektor is still active, and functional!
			"}

		usr << info

/mob/Stat()
	statpanel("Status", "CPU: [world.cpu]")

/world/New()
	..()
	world.log << "World loaded!"
