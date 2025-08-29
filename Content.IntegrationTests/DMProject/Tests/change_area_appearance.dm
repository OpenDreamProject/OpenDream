/area/subtype
	color = rgb(255,0,0)

/proc/test_change_area_appearance()
	var/area/subtype/S = new()
	var/list/block_turfs = block(locate(1,1,1), locate(2,2,2))
	for(var/turf/T in block_turfs)
		S.contents += T

	ASSERT(istype(locate(1,1,1):loc, /area/subtype))
	locate(1,1,1):loc:color = rgb(0,0,0)
	ASSERT(locate(2,2,2):loc:color == rgb(0,0,0))