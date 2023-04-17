/image
	parent_type = /datum

	var/alpha
	var/appearance
	var/appearance_flags
	var/blend_mode = 0
	var/color
	var/desc
	var/gender = "neuter" as opendream_unimplemented
	var/infra_luminosity = 0 as opendream_unimplemented
	var/invisibility as opendream_unimplemented
	var/list/filters = list()
	var/layer
	var/luminosity = 0 as opendream_unimplemented
	var/maptext = "i" as opendream_unimplemented
	var/maptext_width = 32 as opendream_unimplemented
	var/maptext_height = 32 as opendream_unimplemented
	var/maptext_x = 0 as opendream_unimplemented
	var/maptext_y = 0 as opendream_unimplemented
	var/mouse_over_pointer = 0 as opendream_unimplemented
	var/mouse_drag_pointer = 0 as opendream_unimplemented
	var/mouse_drop_pointer = 1 as opendream_unimplemented
	var/mouse_drop_zone = 0 as opendream_unimplemented
	var/mouse_opacity
	var/name = "image"
	var/opacity as opendream_unimplemented
	var/list/overlays = list()
	var/override = 1 as opendream_unimplemented
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_w = 0 as opendream_unimplemented
	var/pixel_z = 0 as opendream_unimplemented
	var/plane
	var/render_source
	var/render_target
	var/suffix as opendream_unimplemented
	var/text = "i" as opendream_unimplemented
	var/matrix/transform
	var/list/underlays = list()
	var/vis_flags = 0 as opendream_unimplemented

	var/bound_width as opendream_unimplemented
	var/bound_height as opendream_unimplemented
	var/x
	var/y
	var/z
	var/list/vis_contents = list() as opendream_unimplemented

	var/dir
	var/icon
	var/icon_state

	var/atom/loc

	// The ref does not mention the pixel_x and pixel_y args...
	New(icon, loc, icon_state, layer, dir, pixel_x, pixel_y)
