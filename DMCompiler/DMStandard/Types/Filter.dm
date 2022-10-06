
/dm_filter
	var/type
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
	const/type = "alpha"


/dm_filter/angular_blur
	const/type = "angular_blur"


/dm_filter/bloom
	const/type = "bloom"

/dm_filter/blur //gaussian blur
	const/type = "blur"

/dm_filter/color
	const/type = "color"


/dm_filter/displace
	const/type = "displace"

/dm_filter/drop_shadow
	const/type = "drop_shadow"

/dm_filter/layer
	const/type = "layer"

/dm_filter/motion_blur
	const/type = "motion_blur"

/dm_filter/outline
	const/type = "outline"

/dm_filter/radial_blur
	const/type = "radial_blur"

/dm_filter/rays
	const/type = "rays"

/dm_filter/ripple
	const/type = "ripple"

/dm_filter/wave
	const/type = "wave"

/dm_filter/greyscale //OD exclusive filter, definitely not just for debugging
	const/type = "greyscale"