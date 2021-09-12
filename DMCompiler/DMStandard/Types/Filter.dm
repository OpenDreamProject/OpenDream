/__filter
	var/type

	New(type, ...)
		CRASH("This filter type [type] is not implemented yet.")

/__filter/blur
	var/size

/__filter/outline
	var/size
	var/color
	var/flag

/__filter/drop_shadow
	var/x
	var/y
	var/size
	var/offset
	var/color

/__filter/alpha
	var/x
	var/y
	var/size
	var/offset
	var/color

/__filter/angular_blur
	var/x
	var/y
	var/size

/__filter/bloom
	var/threshold
	var/size
	var/offset
	var/color

/__filter/color
	var/space

/__filter/displace
	var/x
	var/y
	var/size
	var/icon/icon
	var/render_source

/__filter/layer
	var/x
	var/y
	var/render_source
	var/flags
	var/color
	var/transform
	var/blend_mode

/__filter/motion_blur
	var/x
	var/y

/__filter/outline
	var/size
	var/color
	var/flags

/__filter/radial_blur
	var/x
	var/y
	var/size

/__filter/rays
	var/x
	var/y
	var/size
	var/color
	var/offset
	var/density
	var/threshold
	var/factor
	var/flags

/__filter/ripple
	var/x
	var/y
	var/repeat
	var/radius
	var/falloff
	var/flags

/__filter/wave
	var/x
	var/y
	var/size
	var/offset
	var/flags

proc/filter(...)
	var/list/accepted_filter_types = list(
		"blur" = list("size"),
		"outline" = list("size", "color", "flags"),
		"drop_shadow" = list("x", "y", "size", "offset", "color"),
		"alpha" = list("x", "y", "icon", "render_source", "flags"),
		"angular_blur" = list("x", "y", "size"),
		"bloom" = list("threshold", "size", "offset", "alpha"),
		"color" = list("space"),
		"displace" = list("x", "y", "size", "icon", "render_source"),
		"layer" = list("x", "y", "render_source", "flags", "color", "transform", "blend_mode"),
		"motion_blur" = list("x", "y"),
		"outline" = list("size", "color", "flags"),
		"radial_blur" = list("x", "y", "size"),
		"rays" = list("x", "y", "size", "color", "offset", "density", "threshold", "factor", "flags"),
		"ripple" = list("x", "y", "repeat", "radius", "falloff", "flags"),
		"wave" = list("x", "y", "size", "offset", "flags"),
	)
	var/type = args["type"]
	
	if (isnull(type) || !(type in accepted_filter_types))
		return null // This is how DM does it

	var/list/accepted_filter_args = accepted_filter_types[args["type"]]

	for (var/arg in args)
		if (!(arg in accepted_filter_args))
			CRASH("filter argument [arg] not found")

	return new /__filter(argslist(args))