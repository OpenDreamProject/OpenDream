/__filter
	var/type

	var/size

	var/color

	var/x
	var/y

	var/offset

	var/flags

	var/border

	var/render_source

	var/icon

	var/space

	var/transform

	var/blend_mode

	var/density

	var/threshold

	var/factor

	var/repeat

	var/radius

	var/falloff

	var/alpha

	New(type, ...)
		CRASH("This filter type [type] is not implemented yet.")

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
		"rays" = list("x", "y", "size", "color", "offset", "density", "theshold", "factor", "flags"),
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