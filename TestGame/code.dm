/atom
	Click()
		usr << "You clicked [src.type]"

/turf
	icon = 'icons/turf.dmi'
	icon_state = "turf"
	layer = TURF_LAYER
	plane = -1

/turf/blue
	icon_state = "turf_blue"

/obj/table
	name = "table"
	desc = "It's a table. You can hide under it."
	icon = 'icons/objects.dmi'
	icon_state = "table"
	density = 0
	layer = OBJ_LAYER
	plane = 10000 //top possible plane

/obj/gun
	name = "gun"
	desc = "It doesn't shoot, but it sure looks cool."
	icon = 'icons/objects.dmi'
	icon_state = "gun"
	density = 0
	layer = OBJ_LAYER

	Crossed(var/atom/movable/AM)
		src.loc = AM
		usr << "You picked up [src]"
		AM.overlays += image(src.icon, AM.loc, src.icon_state)


/mob
	icon = 'icons/mob.dmi'
	icon_state = "mob"
	layer = MOB_LAYER
	plane = 5

	New()
		..()
		loc = locate(5, 5, 1)
		color = rgb(rand(0,255), rand(0,255), rand(0,255))

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
		set name = "Tell Location"
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
		set category = "Test"
		alert(usr, "Prepare to die.")
		usr << "prompt done"

	verb/input_num()
		var/v = input("A") as num
		usr << "you entered [v]"

	verb/test_browse()
		set category = "Test"
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
	<script>
	function foo(v) {
		document.getElementById("mark").innerHTML = v;
	}
	</script>
</head>
<body>
	<marquee id="mark">Honk</marquee>
	<a href="?honk=1">click me</a>
</body>
</html>"},"window=honk")

	verb/test_output()
		set category = "Test"
		usr << output("help sec griffing me", "honk.browser:foo")

	verb/demo_filters()
		set category = "Test"
		if(length(src.filters))
			src.filters = null
			usr << "Filters cleared"
		else
			var/selected = input("Pick a filter", "Choose a filter to apply (with demo settings)", null) as null|anything in list("outline", "greyscale", "blur", "outline/grey", "grey/outline", "all")
			if(isnull(selected))
				src.filters = null
				usr << "No filter selected, filters cleared"
			switch(selected)
				if("outline")
					src.filters = filter(type="outline", size=1, color=rgb(255,0,0))
				if("greyscale")
					src.filters = filter(type="greyscale")
				if("blur")
					src.filters = filter(type="blur", size=2)
				if("outline/grey")
					src.filters = list(filter(type="outline", size=1, color=rgb(255,0,0)), filter(type="greyscale"))
				if("grey/outline")
					src.filters = list(filter(type="greyscale"), filter(type="outline", size=1, color=rgb(255,0,0)))
				if("all")
					src.filters = list(filter(type="greyscale"), filter(type="outline", size=1, color=rgb(255,0,0)), filter(type="blur", size=2))
			usr << "Applied [selected] filter"

	verb/test_para()
		set category = "Test"
		src.create_parallax()			

/mob/Stat()
	if (statpanel("Status"))
		stat("tick_usage", world.tick_usage)
		stat("time", world.time)

/world/New()
	..()
	world.log << "World loaded!"
