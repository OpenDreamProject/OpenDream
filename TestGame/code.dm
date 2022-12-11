/datum/complex	 // complex number a+bi
	var/a as num
	var/b as num

	New(_a,_b)
		a = _a
		b = _b

	proc/operator+(datum/complex/C)
		if(istype(C)) return new /datum/complex(a+C.a, b+C.b)
		if(isnum(C)) return new /datum/complex(a+C, b)
		return src

	proc/operator+=(datum/complex/C)
		if(istype(C))
			a += C.a
			b += C.b
		else if(isnum(C)) 
			a += C

	proc/operator-()
		 return new /datum/complex(a*-1, b*-1)
		 
	proc/operator*(datum/complex/C) 
		if(isnum(C))
			return new /datum/complex(a*C, b*C)
		else
			return new /datum/complex((src.a * C.a) - (src.b * C.b), (src.a * C.b) + (src.b * C.a))

	proc/operator[](index)
		switch(index)
			if(1)
				return src.a
			if(2)
				return src.b
			else
				throw EXCEPTION("Invalid index on complex number")

	proc/operator&(datum/complex/C)
		world.log << "OPERATOR RAN &"
		return 3.14

	proc/operator&=(datum/complex/C)
		world.log << "OPERATOR RAN &="
		src.a = 3.14
		src.b = 6.28

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

/mob/Stat()
	if (statpanel("Status"))
		stat("tick_usage", world.tick_usage)
		stat("time", world.time)

/world/New()
	..()
	world.log << "World loaded!"
	var/datum/complex/A = new /datum/complex(5,-1)
	var/datum/complex/B = new /datum/complex(0,-9.5)
	world.log << json_encode(A & B)
	A &= B
	world.log << json_encode(A)