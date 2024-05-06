/atom
	parent_type = /datum

	var/name = null
	var/text = null
	var/desc = null
	var/suffix = null as opendream_unimplemented

	// The initialization/usage of these lists is handled internally by the runtime
	var/tmp/list/verbs = null
	var/list/contents = null
	var/list/overlays = null
	var/list/underlays = null
	var/tmp/list/vis_locs = null as opendream_unimplemented
	var/list/vis_contents = null

	var/tmp/atom/loc
	var/dir = SOUTH
	var/tmp/x = 0
	var/tmp/y = 0
	var/tmp/z = 0
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_z = 0 as opendream_unimplemented
	var/pixel_w = 0 as opendream_unimplemented

	var/icon = null
	var/icon_state = ""
	var/layer = 2.0
	var/plane = 0
	var/alpha = 255
	var/color = "#FFFFFF"
	var/invisibility = 0
	var/mouse_opacity = 1
	var/infra_luminosity = 0 as opendream_unimplemented
	var/luminosity = 0 as opendream_unimplemented
	var/opacity = 0
	var/matrix/transform
	var/blend_mode = 0

	var/gender = NEUTER
	var/density = FALSE

	var/maptext as opendream_unimplemented

	var/list/filters = null
	var/appearance
	var/appearance_flags = 0
	var/maptext_width as opendream_unimplemented
	var/maptext_height as opendream_unimplemented
	var/maptext_x = 32 as opendream_unimplemented
	var/maptext_y = 32 as opendream_unimplemented
	var/step_x as opendream_unimplemented
	var/step_y as opendream_unimplemented
	var/render_source
	var/tmp/mouse_drag_pointer as opendream_unimplemented
	var/tmp/mouse_drop_pointer as opendream_unimplemented
	var/tmp/mouse_over_pointer as opendream_unimplemented
	var/render_target
	var/vis_flags as opendream_unimplemented

	proc/Click(location, control, params)

	proc/DblClick(location, control, params)
		set opendream_unimplemented = TRUE

	proc/MouseDown(location, control, params)
		set opendream_unimplemented = TRUE

	proc/MouseDrag(over_object,src_location,over_location,src_control,over_control,params)
		set opendream_unimplemented = TRUE

	proc/MouseDrop(over_object,src_location,over_location,src_control,over_control,params)

	proc/MouseEntered(location,control,params)
		set opendream_unimplemented = TRUE

	proc/MouseExited(location,control,params)
		set opendream_unimplemented = TRUE

	proc/MouseMove(location,control,params)
		set opendream_unimplemented = TRUE

	proc/MouseUp(location,control,params)
		set opendream_unimplemented = TRUE

	proc/MouseWheel(delta_x,delta_y,location,control,params)
		set opendream_unimplemented = TRUE

	proc/Entered(atom/movable/Obj, atom/OldLoc)
	proc/Exited(atom/movable/Obj, atom/newloc)
	proc/Uncrossed(atom/movable/O)
	proc/Crossed(atom/movable/O)

	proc/Cross(atom/movable/O)
		// Allow crossing only if not both atoms are dense
		return !(src.density && O.density)

	proc/Uncross(atom/movable/O)
		return TRUE

	proc/Enter(atom/movable/O, atom/oldloc)
		return TRUE

	proc/Exit(atom/movable/O, atom/newloc)
		return TRUE

	proc/Stat()
	
	New(loc)
		..()
