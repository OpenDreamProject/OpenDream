#ifndef OPENDREAM
/world/proc/ODHotReloadInterface()
	world.log << "OpenDream-specific procs don't exist in BYOND."
	return

/world/proc/ODHotReloadResource()
	world.log << "OpenDream-specific procs don't exist in BYOND."
	return
#endif

#define TURF_PLANE -10

/obj/plane_master
	appearance_flags = PLANE_MASTER

/obj/plane_master/turf
	screen_loc = "1,1"
	plane = TURF_PLANE

	New()
		src.filters = filter(type="displace", size=100, icon=icon('icons/displace.dmi',"lense"))

/mob/verb/examine(atom/thing as obj|mob in world)
	set category = null
	usr << "This is [thing]. [thing.desc]"

/mob/verb/possess_key(mob/someone as mob in world)
	set category = null
	someone.ckey = usr.key

/turf
	icon = 'icons/turf.dmi'
	icon_state = "turf"
	layer = TURF_LAYER
	plane = TURF_PLANE

/turf/blue
	icon_state = "turf_blue"

/area/withicon
	icon = 'icons/objects.dmi'
	icon_state = "overlay"

	New()
		toggleBlink()

	proc/toggleBlink()
		if (icon == 'icons/objects.dmi')
			icon = null
		else
			icon = 'icons/objects.dmi'
		spawn(20)
			toggleBlink()

/mob
	icon = 'icons/mob.dmi'
	icon_state = "mob"
	layer = MOB_LAYER
	plane = 5
	blend_mode = BLEND_OVERLAY
	name = "Square Man"
	desc = "Such a beautiful smile."
	gender = MALE
	see_invisible = 101

	New()
		..()
		loc = locate(5, 5, 1)

	Login()
		world.log << "login ran"
		src.client.screen += new /obj/order_test_item/plane_master //used for render tests

	verb/winget_test()
		usr << "windows: [json_encode(winget(usr, null, "windows"))]"
		usr << "panes: [json_encode(winget(usr, null, "panes"))]"
		usr << "menus: [json_encode(winget(usr, null, "menus"))]"
		usr << "macros: [json_encode(winget(usr, null, "macros"))]"

	verb/browse_rsc_test()
		usr << browse_rsc('icons/mob.dmi', "mobicon.png")
		usr << browse_rsc('icons/mob.dmi', "mobicon.png")
		usr << browse("<p><img src=mobicon.png></p>Oh look, it's you!","window=honk")

	verb/rotate()
		for(var/i in 1 to 8)
			src.transform = src.transform.Turn(45)
			sleep(2)

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

	verb/show_ohearers()
		var/list/ohearers = ohearers()

		usr << "All hearers: "
		for (var/mob/hearer in ohearers)
			usr << hearer

	verb/start_walk()
		set name = "Walk North"
		usr << "Walking north. Use the 'Walk Stop' verb to cease."
		walk(src, NORTH)

	verb/start_walk_rand()
		set name = "Walk Randomly"
		usr << "Walking randomly. Use the 'Walk Stop' verb to cease."
		walk_rand(src)

	verb/stop_walk()
		set name = "Walk Stop"
		usr << "Walking stopped."
		walk(src, 0)

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

	verb/test_maptext()
		set category = "Test"
		if(length(src.maptext))
			src.maptext = null;
		else
			src.maptext = "Hello!"
			animate(src, maptext_x=64, maptext_y=64, time=50)
			animate(maptext_x=64, maptext_y=-64, time=50)
			animate(maptext_x=-64, maptext_y=-64, time=50)
			animate(maptext_x=-64, maptext_y=64, time=50)
			animate(maptext_x=0, maptext_y=0, time=50)
			animate(maptext="Hello :)", time=10)


	verb/demo_filters()
		set category = "Test"
		if(length(src.filters))
			src.filters = null
			usr << "Filters cleared"
		else
			var/selected = input("Pick a filter", "Choose a filter to apply (with demo settings)", null) as null|anything in list("alpha", "alpha-swap", "alpha-inverse", "alpha-both", "color", "displace", "outline", "greyscale", "blur", "outline/grey", "grey/outline", "drop_shadow")
			if(isnull(selected))
				src.filters = null
				usr << "No filter selected, filters cleared"
			switch(selected)
				if("alpha")
					src.filters = filter(type="alpha", icon=icon('icons/objects.dmi',"checker"))
				if("alpha-swap")
					src.filters = filter(type="alpha", icon=icon('icons/objects.dmi',"checker"), flags=MASK_SWAP)
				if("alpha-inverse")
					src.filters = filter(type="alpha", icon=icon('icons/objects.dmi',"checker"), flags=MASK_INVERSE)
				if("alpha-both")
					src.filters = filter(type="alpha", icon=icon('icons/objects.dmi',"checker"), flags=MASK_INVERSE|MASK_SWAP)
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
				if("color")
					src.filters = filter(type="color", color=list("#de0000","#000000","#00ad00"))
				if("drop_shadow")
					src.filters = filter(type="drop_shadow", size=2)
				if("displace")
					src.client.screen += new /obj/plane_master/turf
			usr << "Applied [selected] filter"

	verb/toggle_see_invisibility()
		if(src.see_invisible == 0)
			src.see_invisible = 101
			usr << "now seeing invisible things"
		else
			src.see_invisible = 0
			usr << "now blind to invisible things"

	verb/add_client_image()
		var/image/i = image(icon = 'icons/hanoi.dmi', icon_state="8")
		i.loc = src
		i.override = 1

		src.client.images += i
		usr << "override added"
		for(var/turf/T in range(src, 2))
			var/image/turf_image = image(icon = 'icons/hanoi.dmi', loc=T, icon_state="1")
			src.client.images += turf_image
		spawn(25)
			src << "changing image"
			i.icon_state = "5"
		spawn(50)
			src.client.images.Cut()

	verb/test_hide_main_window()
		src << "hiding main window"
		winset(src,"mainwindow","is-visible=false")
		spawn(20)
			src << "showing main window"
			winset(src,"mainwindow","is-visible=true")

	verb/winget_text_verb(var/rawtext as command_text)
		set name = "wingettextverb"
		world << "recieved: [rawtext]"

	verb/test_hot_reload_interface()
		set category = "Test"
		src << "trying hot reload of interface..."
		world.ODHotReloadInterface()
		src << "done hot reload of interface!"

	verb/test_hot_reload_icon()
		set category = "Test"
		src << "trying hot reload of icon..."
		world.ODHotReloadResource("icons/turf.dmi")
		src << "done hot reload of icon!"

	verb/manipulate_world_size()
		var/x = input("New World X?", "World Size", 0) as num|null
		var/y = input("New World Y?", "World Size", 0) as num|null

		if(!x || !y)
			return

		world.maxx = x
		world.maxy = y

	verb/toggle_show_popups()
		client.show_popup_menus = !client.show_popup_menus
		src << "Popups are now [client.show_popup_menus ? "enabled" : "disabled"]"

	// input() test		
	verb/text_test()
		set category = "Input Test"
		src << input("Input test: text") as text|null
	
	// todo: implement
	// verb/password_test()
	// 	set category = "Input Test"
	// 	src << input("Input test: password") as password|null
		
	verb/multiline_test()
		set category = "Input Test"
		src << input("Input test: message") as message|null
		
	verb/command_test()
		set category = "Input Test"
		src << input("Input test: command_text") as command_text|null
		
	verb/num_test()
		set category = "Input Test"
		src << input("Input test: num") as num|null
		
	verb/icon_test()
		set category = "Input Test"
		src << input("Input test: icon") as icon|null
		
	verb/sound_test()
		set category = "Input Test"
		src << input("Input test: sound") as sound|null
		
	verb/file_test()
		set category = "Input Test"
		src << input("Input test: file") as file|null
		
	// todo: implement
	// verb/key_test()
	// 	set category = "Input Test"
	// 	src << input("Input test: key") as key|null
		
	verb/color_test()
		set category = "Input Test"
		src << input("Input test: color") as color|null
		
	verb/list_test()
		set category = "Input Test"
		src << input("Input test: list") as null|anything in list("option 1", "option 2", "option 3", "option 4", "option 5")
		

/mob/Stat()
	if (statpanel("Status"))
		stat("tick_usage", world.tick_usage)
		stat("time", world.time)

/world/New()
	..()
	world.log << "World loaded!"
