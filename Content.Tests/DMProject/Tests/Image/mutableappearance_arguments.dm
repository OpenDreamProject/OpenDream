/mutable_appearance/New(var/to_copy)
	..()

/proc/RunTest()
	// /mutable_appearance/New()'s args are special in that named arguments ignore any overrides
	// Normally this wouldn't be allowed since the override only has to_copy
	var/mutable_appearance/MA = new(icon = 'icons.dmi', icon_state = "mob")
	
	ASSERT(MA.icon == 'icons.dmi')
	ASSERT(MA.icon_state == "mob")