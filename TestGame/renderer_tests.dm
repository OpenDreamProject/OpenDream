//test object base classes
#define TALL_OBJECT_PLANE 10

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

/obj/plaque/render_source_test 
	data = "<h3>Render source Test</h3><p>Click the button to toggle set the render_source to the render_target of your mob</p>"

/obj/button/render_source_test
	name = "Render Source Test"
	desc = "Click me to set the render_source of the button to your mob's render_target!"

	push()
		src.render_source = usr.render_target
		usr << "Render target set to your mob's render source"	