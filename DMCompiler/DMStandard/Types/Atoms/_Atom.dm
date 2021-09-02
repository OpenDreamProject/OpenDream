/atom
	parent_type = /datum

	var/name = "atom"
	var/desc = null
	var/suffix = null
	var/list/verbs = list()

	var/list/contents = list()
	var/list/overlays = list()
	var/list/underlays = list()
	var/atom/loc
	var/dir = SOUTH
	var/x = 0
	var/y = 0
	var/z = 0
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_z = 0
	var/pixel_w = 0

	var/icon = null
	var/icon_state = ""
	var/layer = 2.0
	var/plane = FLOAT_PLANE
	var/alpha = 255
	var/color = "#FFFFFF"
	var/invisibility = 0
	var/mouse_opacity = 1
	var/luminosity = 0
	var/opacity = 0
	var/matrix/transform
	var/blend_mode = 0

	var/gender = "neuter"
	var/density = FALSE

	var/maptext //TODO

	proc/Click(location, control, params)

	proc/Entered(atom/movable/Obj, atom/OldLoc)
	proc/Exited(atom/movable/Obj, atom/newloc)
	proc/Uncrossed(atom/movable/O)
	proc/Crossed(atom/movable/O)

	proc/Cross(atom/movable/O)
		return !(src.density && O.density)

	proc/Uncross(atom/movable/O)
		return TRUE

	proc/Enter(atom/movable/O, atom/oldloc)
		return TRUE

	proc/Exit(atom/movable/O, atom/newloc)
		return TRUE
	
	proc/Stat()
