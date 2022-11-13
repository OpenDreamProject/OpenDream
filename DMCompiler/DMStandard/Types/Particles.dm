//TODO: Figure out how particles work internally
//See: https://www.byond.com/docs/ref/#/{notes}/particles
/particles
	parent_type = /datum
	//Particle vars that affect the entire set (generators are not allowed for these)
	var/width = 100 as opendream_unimplemented //null defaults to 0. width is the size of the particle "image" ie particles within this width image will be rendered, if they are partially in they get partially cut. if they reenter this area after leaving it they reapper. image is centered on particle owner.
	var/height = 100 as opendream_unimplemented //ditto
	var/count = 100 as opendream_unimplemented // if null, uses the last set value. is checked BEFORE lifespan so (count 10, lifespan 10, spawning 1) will skip a pixel every 10 pixels
	var/spawning = 1 as opendream_unimplemented // null is treated as 0
	var/bound1 = -1000 as opendream_unimplemented // Usually list but if a number treated as list(bound1, bound1, bound1). if particles go above/below bound they will get immediately deleted regardless of lifespan. null is treated as the default value (-1000 and 1000)(this could be treated as infinity as well but 1000 is so large its hard to tell)
	var/bound2 = 1000 as opendream_unimplemented // Ditto!
	var/gravity as opendream_unimplemented // Usually list but if a number treated as list(gravity, gravity, gravity).
	var/list/gradient = null as opendream_unimplemented // not cast as a list on byond as of 514.1580 despite only being able to be a list
	var/transform as opendream_unimplemented // matrix or list. list can be simple matrix, complex matrix or projection matrix. thus: list(a, b, c, d, e, f) OR list(xx,xy,xz, yx,yy,yz, zx,zy,zz) OR list(xx,xy,xz, yx,yy,yz, zx,zy,zz, cx,cy,cz) OR list(xx,xy,xz,xw, yx,yy,yz,yw, zx,zy,zz,zw, wx,wy,wz,ww)

	//Vars that apply when a particle spawns
	var/lifespan as opendream_unimplemented  // actual time a particle exists is fadein + lifespan + fade. thus this just the time it spends fully faded in. null is treated as 
	var/fade as opendream_unimplemented // null treated as 0
	var/fadein as opendream_unimplemented // null treated as 0
	var/icon as opendream_unimplemented // either icon or list(icon = weightofthisicon, icon = weightofthisicon) if null defaults to a 1x1 white pixel
	var/icon_state as opendream_unimplemented // either string or list(string = weightofthisiconstate, string = weightofthisiconstate) if null defaults to a 1x1 white pixel
	var/color as opendream_unimplemented // null treated as 0
	var/color_change as opendream_unimplemented // null treated as 0
	var/position as opendream_unimplemented // Usually list but if a number treated as list(position, position, position). null is treated as 0
	var/velocity as opendream_unimplemented // Usually list but if a number treated as list(velocity, velocity, velocity). null is treated as 0
	var/scale as opendream_unimplemented // if null defaults to 1, if number treated as list(scale, scale)
	var/grow as opendream_unimplemented // if null defaults to 0, if number treated as list(grow, grow)
	var/rotation as opendream_unimplemented // null treated as 0
	var/spin as opendream_unimplemented // null treated as 0
	var/friction as opendream_unimplemented // null treated as 0, numbers below 0 treated as 0

	//Vars that are evaluated every tick
	var/drift as opendream_unimplemented // Usually list but if a number treated as list(drift, drift, drift)

//misc notes
// particle image height/width is not considered for TILE_BOUND-less atoms
// later spawned particles layer over sooner spawned particles
// no idea how z works, except that by default going under -1 and over 200 makes the particle vanish
// in byond, particles only get updated when a new var is set, so doing things like replacing something in a list doesnt update it
