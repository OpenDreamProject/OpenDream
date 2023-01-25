#define PLANE_SPACE -95
#define PLANE_SPACE_PARALLAX -90


/client
	var/list/parallax_layers
	var/list/parallax_layers_cached
	var/static/list/parallax_static_layers_tail = newlist(/obj/screen/parallax_pmaster, /obj/screen/parallax_space_whitifier)
	var/atom/movable/movingmob
	var/turf/previous_turf
	var/dont_animate_parallax //world.time of when we can state animate()ing parallax again
	var/last_parallax_shift //world.time of last update
	var/parallax_throttle = 0 //ds between updates
	var/parallax_movedir = 0
	var/parallax_layers_max = 4
	var/parallax_animate_timer

/mob/proc/create_parallax()
	var/client/C = src.client

	if(!length(C.parallax_layers_cached))
		C.parallax_layers_cached = list()
		C.parallax_layers_cached += new /obj/screen/parallax_layer/layer_1(null, C.view)
		C.parallax_layers_cached += new /obj/screen/parallax_layer/layer_2(null, C.view)
		C.parallax_layers_cached += new /obj/screen/parallax_layer/planet(null, C.view)
		C.parallax_layers_cached += new /obj/screen/parallax_layer/layer_3(null, C.view)

	C.parallax_layers = C.parallax_layers_cached.Copy()
	C.parallax_throttle = 0
	C.parallax_layers_max = 4
	if(length(C.parallax_layers) > C.parallax_layers_max)
		C.parallax_layers.len = C.parallax_layers_max

	C.screen |= (C.parallax_layers + C.parallax_static_layers_tail)


/obj/screen/parallax_layer
	icon = 'icons/effects/parallax.dmi'
	var/speed = 1
	var/offset_x = 0
	var/offset_y = 0
	var/view_sized
	var/absolute = FALSE
	blend_mode = BLEND_ADD
	plane = PLANE_SPACE_PARALLAX
	screen_loc = "CENTER-7,CENTER-7"
	mouse_opacity = 0


/obj/screen/parallax_layer/New(view)
	..()
	if(!view)
		view = world.view
	update_o(view)

/obj/screen/parallax_layer/proc/update_o(view)
	if(!view)
		view = world.view
	var/list/new_overlays = list()
	var/count = round(view/(480/world.icon_size), 1)+1
	for(var/x in -count to count)
		for(var/y in -count to count)
			if(x == 0 && y == 0)
				continue
			var/mutable_appearance/texture_overlay = new(image(icon, icon_state))
			texture_overlay.transform = matrix(1, 0, x*480, 0, 1, y*480)
			new_overlays += texture_overlay

	overlays = new_overlays
	view_sized = view

/obj/screen/parallax_layer/proc/update_status(mob/M)
	return

/obj/screen/parallax_layer/layer_1
	icon_state = "layer1"
	speed = 0.6
	layer = 1

/obj/screen/parallax_layer/layer_2
	icon_state = "layer2"
	speed = 1
	layer = 2

/obj/screen/parallax_layer/layer_3
	icon_state = "layer3"
	speed = 1.4
	layer = 3

/obj/screen/parallax_layer/random
	blend_mode = BLEND_OVERLAY
	speed = 3
	layer = 3

/obj/screen/parallax_layer/random/space_gas
	icon_state = "space_gas"

/obj/screen/parallax_layer/random/space_gas/New(view)
	..()

/obj/screen/parallax_layer/random/asteroids
	icon_state = "asteroids"
	layer = 4

/obj/screen/parallax_layer/planet
	icon_state = "planet"
	blend_mode = BLEND_OVERLAY
	absolute = TRUE //Status of seperation
	speed = 3
	layer = 30

/obj/screen/parallax_layer/planet/update_status(mob/M)
	invisibility = 0

/obj/screen/parallax_layer/planet/update_o()
	return //Shit wont move

/obj/screen/parallax_pmaster
	appearance_flags = PLANE_MASTER
	plane = PLANE_SPACE_PARALLAX
	blend_mode = BLEND_MULTIPLY
	mouse_opacity = FALSE
	screen_loc = "CENTER-7,CENTER-7"

/obj/screen/parallax_space_whitifier
	appearance_flags = PLANE_MASTER
	plane = PLANE_SPACE
	color = list(
		0, 0, 0, 0,
		0, 0, 0, 0,
		0, 0, 0, 0,
		1, 1, 1, 1,
		0, 0, 0, 0
		)
	screen_loc = "CENTER-7,CENTER-7"
