/world/New()
	..()
	var/list/block_turfs = block(locate(1,1,1), locate(2,2,2))
	var/list/block_coords = block(1,1,1,2,2,2)
	
	ASSERT(block_turfs ~= block_coords)
	
	var/list/block_same = block(1,1,1,1,1,1)
	var/list/block_weird = block(1,1,1,"cat",null,/turf)
	
	ASSERT(block_same ~= block_weird)
	
	var/list/block_diff_y = block(1,1,1,1,2,1)
	var/list/block_incomplete = block(1,1,1,1,2)
	
	ASSERT(block_diff_y ~= block_incomplete)
	
	var/list/block_invalid = block(7,7,7,8,8,8)
	
	ASSERT(block_invalid ~= list())