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

	verb/test_alert()
		alert(usr, "Prepare to die.")
		usr << "prompt done"

	verb/input_num()
		var/v = input("A") as num
		usr << "you entered [v]"

	verb/test_browsersc()
		usr << browse_rsc('icons/mob.dmi')

	verb/test_browse()
		usr << browse({"
<!DOCTYPE html>
<html>
<head>
	<title>Foo</title>
	<style>
	body {
		background: red;
	}
	</style>
</head>
<body>
	<marquee>Honk</marquee>
	<a href="?honk=1">click me</a>
</body>
</html>"},"window=honk")

/mob/Stat()
	statpanel("Status", "CPU: [world.cpu]")
	stat("time", world.time)

/client/Click(var/atom/A)
	..()
	Move(A, get_dir(mob, A))

/world/New()
	..()
	world.log << "World loaded!"

/client/Topic(href,href_list,hsrc)
	usr << href
	usr << json_encode(href_list)

	..()
