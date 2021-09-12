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



proc/filter(type, size,)
	return new /__filter(type, size, color, x, y, offset, flags, border, render_source, icon, space, transform, blend_mode, density, factor, repeat, radius, falloff, alpha)