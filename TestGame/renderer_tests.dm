//test object base classes
#define TALL_OBJECT_PLANE 10
#define PLANE_LIGHTING 50

#define PLANE_hanoi_0 20
#define PLANE_hanoi_1 21
#define PLANE_hanoi_3 23
#define PLANE_hanoi_4 24
#define PLANE_hanoi_7 27
#define PLANE_hanoi_8 28
#define PLANE_hanoi_9 29


/obj/plaque
	var/data = "Empty plaque"
	icon = 'icons/objects.dmi'
	icon_state = "plaque"
	layer = OBJ_LAYER	

	Click()
		usr << browse(data, "window=plaquepopup")
	

/obj/button
	name = "Button"
	desc = "Push me!"
	icon = 'icons/objects.dmi'
	icon_state = "button"
	var/pushed = 0

	Click()
		. = ..()
		if(!pushed)
			src.icon_state = "buttonpush"
			src.push()
			src.pushed = 1
			spawn(10)
				src.icon_state = "button"
				src.pushed = 0

	proc/push()
		usr << "You pushed the button"
		

//--------------------------------------------------------------------------
// Test objects begin here
//---------------------------------------------------------------------------

// Simple plane
/obj/plaque/simple_plane_test 
	data = "<h3>Simple Plane Test</h3><p>Move under the table to hide under it's higher plane value</p>"
/obj/table
	name = "table"
	desc = "It's a table. You can hide under it."
	icon = 'icons/objects.dmi'
	icon_state = "table"
	density = 0
	layer = OBJ_LAYER
	plane = TALL_OBJECT_PLANE


// Simple overlay
/obj/plaque/simple_overlay_test 
	data = "<h3>Simple Overlay Test</h3><p>Pick up the gun to apply it as an overlay to your mob</p>"
/obj/gun
	name = "gun"
	desc = "It doesn't shoot, but it sure looks cool."
	icon = 'icons/objects.dmi'
	icon_state = "gun"
	density = 0
	layer = OBJ_LAYER

	Crossed(var/atom/movable/AM)
		src.loc = AM
		AM << "You picked up [src]"
		AM.overlays += image(src.icon, AM.loc, src.icon_state)	

//simple underlay
/obj/plaque/simple_underlay_test 
	data = "<h3>Simple Underlay Test</h3><p>Pick up the bandoleer to apply it as an underlay to your mob</p>"
/obj/bandoleer
	name = "bandoleer"
	desc = "For holding all the bullets your gun doesn't shoot"
	icon = 'icons/objects.dmi'
	icon_state = "bandoleer"
	density = 0
	layer = OBJ_LAYER

	Crossed(var/atom/movable/AM)
		src.loc = AM
		AM << "You picked up [src]"
		AM.underlays += image(src.icon, AM.loc, src.icon_state)	

// Blend modes
/obj/plaque/blend_mode_test 
	data = "<h3>Blend Mode Test</h3><p>Click the button to switch your mob's blend mode</p>"

/obj/button/blend_mode_test
	name = "Blend Mode Test"
	desc = "Click me to switch blend modes!"

	push()
		switch(usr.blend_mode)
			if(BLEND_DEFAULT)
				usr.blend_mode = BLEND_OVERLAY
				usr << "BLEND_OVERLAY"
			if(BLEND_OVERLAY)
				usr.blend_mode = BLEND_ADD
				usr << "BLEND_ADD"
			if(BLEND_ADD)
				usr.blend_mode = BLEND_SUBTRACT
				usr << "BLEND_SUBTRACT"	
			if(BLEND_SUBTRACT)
				usr.blend_mode = BLEND_MULTIPLY
				usr << "BLEND_MULTIPLY"
			if(BLEND_MULTIPLY)
				usr.blend_mode = BLEND_INSET_OVERLAY
				usr << "BLEND_INSET_OVERLAY"
			if(BLEND_INSET_OVERLAY)
				usr.blend_mode = BLEND_DEFAULT
				usr << "BLEND_DEFAULT"

// transforms
/obj/plaque/transform_rotate_test 
	data = "<h3>Transform Rotate Test</h3><p>Click the button to R O T A T E</p>"

/obj/button/transform_rotate_test
	name = "Transform Rotate Test"
	desc = "Click me to rotate!"

	push()
		usr.transform = turn(usr.transform, 45)

/obj/plaque/transform_scale_test 
	data = "<h3>Transform Scale Test</h3><p>Click the button to LARGE</p>"

/obj/button/transform_scale_test
	name = "Transform Scale Test"
	desc = "Click me to enlarge!"

	push()
		usr.transform *= 2		

/obj/plaque/transform_scale_small_test 
	data = "<h3>Transform Scale Test</h3><p>Click the button to small</p>"

/obj/button/transform_scale_small_test
	name = "Transform Scale Test"
	desc = "Click me to ensmall!"

	push()
		usr.transform *= 0.5	

/obj/plaque/transform_translate_test 
	data = "<h3>Transform Translate Test</h3><p>Click the button to shift or shift back</p>"

/obj/button/transform_translate_test
	name = "Transform Translate Test"
	desc = "Click me to be translated"

	push()
		usr.transform = matrix(32, 6, MATRIX_TRANSLATE)			
	
//keep together groups
/obj/plaque/keep_together_test 
	data = "<h3>KEEP_TOGETHER Test</h3><p>Click the button to toggle KEEP_TOGETHER as an appearance flag on your mob</p>"

/obj/button/keep_together_test
	name = "KEEP_TOGETHER Test"
	desc = "Click me to toggle keep together!"

	push()
		usr.appearance_flags ^= KEEP_TOGETHER
		usr << "KEEP_TOGETHER IS [usr.appearance_flags & KEEP_TOGETHER ? "TRUE" : "FALSE"]"

/obj/plaque/keep_apart_test 
	data = "<h3>KEEP_APART Test</h3><p>Click the button to add an overlay with KEEP_APART to your mob</p>"

/obj/keep_apart_obj
	name = "keep apart obj"
	appearance_flags = KEEP_APART | RESET_ALPHA | RESET_COLOR | RESET_TRANSFORM
	icon = 'icons/objects.dmi'
	icon_state = "keepapart"

/obj/button/keep_apart_test
	name = "KEEP_APART Test"
	desc = "Click me to get a KEEP_APART overlay!"

	push()
		usr.overlays += new /obj/keep_apart_obj()
//render sources
/obj/plaque/render_source_test 
	data = "<h3>Render source Test</h3><p>Click the button to toggle set the render_source to the render_target of your mob</p>"

/obj/button/render_source_test
	name = "Render Source Test"
	desc = "Click me to set the render_source of the button to your mob's render_target!"

	push()
		usr.render_target = "\ref[usr]"
		src.render_source = usr.render_target

		usr << "Render target set to your mob's render source"	

//screen objects
/obj/background_image
	icon = 'icons/background.dmi'
	icon_state = "opendream"
	plane = -90
	screen_loc = "CENTER-7:7,CENTER-7:7"

/obj/plaque/screen_background_test 
	data = "<h3>Screen Background Test</h3><p>Click the button to activate the background image</p>"

/obj/button/screen_background_test
	name = "Screen Background Test"
	desc = "Click me to activate a background image"

	push()
		usr.client.screen |= new /obj/background_image()

//planes
/obj/lighting_plane
	screen_loc = "1,1"
	plane = PLANE_LIGHTING
	blend_mode = BLEND_MULTIPLY
	appearance_flags = PLANE_MASTER | NO_CLIENT_COLOR
	color = list(null, null, null, "#0000", "#333f") 
	mouse_opacity = 0 

/image/spotlight
	plane = PLANE_LIGHTING
	blend_mode = BLEND_ADD
	appearance_flags = RESET_COLOR | RESET_ALPHA | RESET_TRANSFORM
	icon = 'icons/spotlight.dmi'  // a 96x96 white circle
	icon_state = "spotlight"
	pixel_x = -32
	pixel_y = -32		

/obj/plaque/plane_master_test 
	data = "<h3>Plane Master Test</h3><p>Click the button to activate the lighting plane master</p>"

/obj/button/plane_master_test
	name = "Plane Master Test"
	desc = "Click me to activate the lighting plane master"

	push()
		usr.client.screen |= new /obj/lighting_plane()	
		usr.overlays += new /image/spotlight()

//render sources for filters
/obj/plaque/alpha_rendersource_test 
	data = "<h3>Alpha Filter RenderSource Test</h3><p>Click the button to add an alpha filter with this button as the render source</p>"

/obj/button/alpha_rendersource_test
	name = "Alpha Filter RenderSource Test"
	desc = "Click me to add alpha filter"
	render_target = "button"

	push()
		usr.filters = filter(type="alpha", render_source="button")

// Color
/obj/plaque/color_matrix_test
	data = "<h3>Color Matrix Test</h3><p>Click the button to apply inverse color matrix to your mob</p>"

/obj/button/color_matrix_test
	name = "Color Matrix Test"
	desc = "Click me to apply color matrix!"

	push()
		usr.color = list(-1,0,0,0, -1,0,0,0, -1,1,1,1)

/obj/plaque/color_test
	data = "<h3>Color Test</h3><p>Click the button to apply random color to your mob</p>"

/obj/button/color_test
	name = "Color Test"
	desc = "Click me to apply a random color!"

	push()
		usr.color = rgb(rand(0,255), rand(0,255), rand(0,255))

/obj/button/animation_test
	name = "Animation Test"
	desc = "Click me to get thouroughly animated!"
	var/i = 0

	push()
		if(i==0)
			//grow and fade
			usr << "grow and fade"
			animate(usr, transform = matrix()*2, alpha = 0, time = 5)
			animate(transform = matrix(), alpha = 255, time = 5)
			sleep(5)
		if(i==1)
			//spin
			usr << "spin"
			animate(usr, transform = turn(matrix(), 120), time = 2, loop = 5)
			animate(transform = turn(matrix(), 240), time = 2)
			animate(transform = null, time = 2)
			sleep(14)
		if(i==2)
			//colour
			usr << "colour"
			animate(usr, color="#ff0000", time=5)
			animate(color="#00ff00", time=5)
			animate(color="#0000ff", time=5)
			animate(color="#ffffff", time=5)
			sleep(20)
		if(i==3)
			//colour matrix
			usr << "colour matrix"
			animate(usr, color=list(0,0,1,0, 1,0,0,0, 0,1,0,0, 0,0,0,1, 0,0,0,0), time=5)
			animate(color=list(0,1,0,0, 0,0,1,0, 1,0,0,0, 0,0,0,1, 0,0,0,0), time=5)
			animate(color=list(1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1, 0,0,0,0), time=5)
			sleep(15)
		if(i==4)
			//parallel
			usr << "parallel"
			animate(usr, color="#ff0000", time=4)
			animate(usr, transform = turn(matrix(), 120), time = 2, flags=ANIMATION_PARALLEL)
			animate(transform = turn(matrix(), 240), time = 2)
			animate(color="#ffffff", transform = null, time = 2)
			sleep(6)
		if(i==5)
			//easings
			usr << "easings"
			animate(usr, transform = matrix()*2, time = 5, easing=BACK_EASING)
			animate(transform = matrix(), time = 5, easing=BOUNCE_EASING)
			animate(transform = matrix()*2, time = 5, easing=ELASTIC_EASING)
			animate(transform = matrix(), time = 5, easing=QUAD_EASING)
			animate(transform = matrix()*2, time = 5, easing=CUBIC_EASING)
			animate(transform = matrix(), time = 5, easing=SINE_EASING)
			animate(transform = matrix()*2, time = 5, easing=CIRCULAR_EASING)
			animate(transform = matrix(), time = 5, easing=JUMP_EASING)
		i++
		if(i>5)
			i = 0
/obj/plaque/animation_test 
	data = "<h3>Animation Test</h3><p>Click the button to apply a series of animations to your mob</p>"

//render order sanity checks
/obj/order_test
	icon = 'icons/hanoi.dmi'
	icon_state = "0"
	name = "Render order test"
	desc = "If this isn't a nice set of 10 squares stacked on top of eachother, something has gone wrong"
	layer = OBJ_LAYER
	plane = PLANE_hanoi_0

	New()
		src.overlays += new /obj/order_test_item/one()
		src.overlays += new /obj/order_test_item/two()
		new /obj/order_test_item/four(src.loc)
		new /obj/order_test_item/five(src.loc)
		new /obj/order_test_item/seven(src.loc)
		new /obj/order_test_item/three(src.loc)
		new /obj/order_test_item/six(src.loc)
		new /obj/order_test_item/nine(src.loc)
		new /obj/order_test_item/render_seven(locate(1,1,1))
		new /obj/order_test_item/eight(src.loc)

/obj/order_test_target
	icon = 'icons/hanoi.dmi'
	icon_state = "target"
	name = "Render order test target"
	desc = "This is what the render order test should look like. If it doesn't, it's wrong."
	layer = OBJ_LAYER
	plane = PLANE_hanoi_0

/obj/order_test_item
	icon = 'icons/hanoi.dmi'
	
/obj/order_test_item/one //make sure planes apply properly
	icon_state = "1"
	plane = PLANE_hanoi_1

/obj/order_test_item/two //test FLOAT_PLANE
	icon_state = "2"
	plane = FLOAT_PLANE+2	

/obj/order_test_item/plane_master //plane master test
	screen_loc = "1,1"
	appearance_flags = PLANE_MASTER
	plane = PLANE_hanoi_3

/obj/order_test_item/three //plane master test
	icon_state = "3"
	plane = PLANE_hanoi_3

/obj/order_test_item/four //layer test
	icon_state = "4"
	plane = PLANE_hanoi_4
	layer = BACKGROUND_LAYER

/obj/order_test_item/five //layer test
	icon_state = "5"
	plane = PLANE_hanoi_4	
	layer = 1

/obj/order_test_item/six //layer test
	icon_state = "6"
	plane = PLANE_hanoi_4	
	layer = EFFECTS_LAYER

/obj/order_test_item/seven //render source/target test
	render_source = "hanoi7"
	plane = PLANE_hanoi_7	

/obj/order_test_item/render_seven //render source/target test
	render_target = "hanoi7"
	icon_state = "7"
	plane = -100	//render below everything

/obj/order_test_item/eight //image test
	icon_state = ""
	plane = PLANE_hanoi_8

	New()
		src.overlays += image(icon = 'icons/hanoi.dmi', icon_state="8")

/obj/order_test_item/nine //image test
	icon_state = "9"
	plane = PLANE_hanoi_9
	invisibility = 99		


/obj/complex_overlay_test
	name = "complex overlay test"
	icon = 'icons/hanoi.dmi'
	icon_state = "5"

	New()
		var/image/zero = image(icon = 'icons/hanoi.dmi', icon_state="0")
		var/image/one = image(icon = 'icons/hanoi.dmi', icon_state="1")
		var/image/two = image(icon = 'icons/hanoi.dmi', icon_state="2")
		var/image/three = image(icon = 'icons/hanoi.dmi', icon_state="3")
		var/image/four = image(icon = 'icons/hanoi.dmi', icon_state="4")
		var/image/six = image(icon = 'icons/hanoi.dmi', icon_state="6")
		var/image/seven = image(icon = 'icons/hanoi.dmi', icon_state="7")
		var/image/eight = image(icon = 'icons/hanoi.dmi', icon_state="8")
		var/image/nine = image(icon = 'icons/hanoi.dmi', icon_state="9")

		one.underlays += zero
		two.underlays += one
		four.underlays += three
		two.overlays += four
		src.underlays += two
		src.overlays += six
		eight.underlays += seven
		eight.overlays += nine
		src.overlays += eight

/obj/float_layer_test
	name = "float layer test"
	icon = 'icons/hanoi.dmi'
	icon_state = "5"

	New()
		var/image/zero = image(icon = 'icons/hanoi.dmi', icon_state="0", layer=FLOAT_LAYER-10)
		var/image/one = image(icon = 'icons/hanoi.dmi', icon_state="1", layer=FLOAT_LAYER-9)
		var/image/two = image(icon = 'icons/hanoi.dmi', icon_state="2", layer=FLOAT_LAYER-8)
		var/image/three = image(icon = 'icons/hanoi.dmi', icon_state="3", layer=FLOAT_LAYER-7)
		var/image/four = image(icon = 'icons/hanoi.dmi', icon_state="4", layer=FLOAT_LAYER-6)
		var/image/six = image(icon = 'icons/hanoi.dmi', icon_state="6", layer=FLOAT_LAYER-5)
		var/image/seven = image(icon = 'icons/hanoi.dmi', icon_state="7", layer=FLOAT_LAYER-4)
		var/image/eight = image(icon = 'icons/hanoi.dmi', icon_state="8", layer=FLOAT_LAYER-3)
		var/image/nine = image(icon = 'icons/hanoi.dmi', icon_state="9", layer=FLOAT_LAYER-2)

		src.underlays += zero
		src.underlays += one
		src.underlays += two
		src.underlays += three
		src.underlays += four
		src.overlays += six
		src.overlays += seven
		src.overlays += eight
		src.overlays += nine
		
