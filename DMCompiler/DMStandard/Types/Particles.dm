//TODO: Figure out how particles work internally
//See: https://www.byond.com/docs/ref/#/{notes}/particles
/particles
	parent_type = /datum
	//Particle vars that affect the entire set (generators are not allowed for these)
	var/width = 100  //null defaults to 0. width is the size of the particle "image" ie particles within this width image will be rendered, if they are partially in they get partially cut. if they reenter this area after leaving it they reapper. image is centered on particle owner.
	var/height = 100  //ditto
	var/count = 100  // if null, uses the last set value. is checked BEFORE lifespan so (count 10, lifespan 10, spawning 1) will skip a pixel every 10 pixels
	var/spawning = 1  // null is treated as 0
	var/bound1 = -1000  // Usually list but if a number treated as list(bound1, bound1, bound1). if particles go above/below bound they will get immediately deleted regardless of lifespan. null is treated as the default value (-1000 and 1000)(this could be treated as infinity as well but 1000 is so large its hard to tell)
	var/bound2 = 1000  // Ditto!
	var/gravity  // Usually list but if a number treated as list(gravity, gravity, gravity).
	var/list/gradient = null  // not cast as a list on byond as of 514.1580 despite only being able to be a list
	var/transform  // matrix or list. list can be simple matrix, complex matrix or projection matrix. thus: list(a, b, c, d, e, f) OR list(xx,xy,xz, yx,yy,yz, zx,zy,zz) OR list(xx,xy,xz, yx,yy,yz, zx,zy,zz, cx,cy,cz) OR list(xx,xy,xz,xw, yx,yy,yz,yw, zx,zy,zz,zw, wx,wy,wz,ww)

	//Vars that apply when a particle spawns
	var/lifespan   // actual time a particle exists is fadein + lifespan + fade. thus this just the time it spends fully faded in. null is treated as 
	var/fade  // null treated as 0
	var/fadein  // null treated as 0
	var/icon  // either icon or list(icon = weightofthisicon, icon = weightofthisicon) if null defaults to a 1x1 white pixel
	var/icon_state  // either string or list(string = weightofthisiconstate, string = weightofthisiconstate) if null defaults to a 1x1 white pixel
	var/color  // null treated as 0
	var/color_change  // null treated as 0
	var/position  // Usually list but if a number treated as list(position, position, position). null is treated as 0
	var/velocity  // Usually list but if a number treated as list(velocity, velocity, velocity). null is treated as 0
	var/scale  // if null defaults to 1, if number treated as list(scale, scale)
	var/grow  // if null defaults to 0, if number treated as list(grow, grow)
	var/rotation  // null treated as 0
	var/spin  // null treated as 0
	var/friction  // null treated as 0, numbers below 0 treated as 0

	//Vars that are evaluated every tick
	var/drift  // Usually list but if a number treated as list(drift, drift, drift)

//misc notes
// particle image height/width is not considered for TILE_BOUND-less atoms
// later spawned particles layer over sooner spawned particles
// no idea how z works, except that by default going under -1 and over 200 makes the particle vanish
// in byond, particles only get updated when a new var is set, so doing things like replacing something in a list doesnt update it
