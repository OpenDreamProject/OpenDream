
/dm_filter
	var/x
	var/y
	var/icon
	var/render_source
	var/flags
	var/size
	var/threshold
	var/offset
	var/alpha
	var/color
	var/space
	var/transform
	var/blend_mode
	var/factor
	var/density
	var/repeat
	var/radius
	var/falloff

/dm_filter/alpha
	type = "alpha"


/dm_filter/angular_blur
	type = "angular_blur"


/dm_filter/bloom
	type = "bloom"

/dm_filter/blur //gaussian blur
	type = "blur"

/dm_filter/color
	type = "color"


/dm_filter/displace
	type = "displace"

/dm_filter/drop_shadow
	type = "drop_shadow"

/dm_filter/layer
	type = "layer"

/dm_filter/motion_blur
	type = "motion_blur"

/dm_filter/outline
	type = "outline"

/dm_filter/radial_blur
	type = "radial_blur"

/dm_filter/rays
	type = "rays"

/dm_filter/ripple
	type = "ripple"

/dm_filter/wave
	type = "wave"

/dm_filter/greyscale //OD exclusive filter, definitely not just for debugging
	type = "greyscale"
