//TODO: Figure out how particles work internally
//See: https://www.byond.com/docs/ref/#/{notes}/particles
/particles
	parent_type = /datum
	//Particle vars that affect the entire set (generators are not allowed for these)
	var/width = 100 as opendream_unimplemented
	var/height = 100 as opendream_unimplemented
	var/count = 100 as opendream_unimplemented
	var/spawning = 1 as opendream_unimplemented
	var/bound1 = -1000 as opendream_unimplemented // While the ref says this is a list, it actually defaults to this number
	var/bound2 = 1000 as opendream_unimplemented // Ditto!
	var/gravity as opendream_unimplemented
	var/list/gradient = null as opendream_unimplemented
	var/matrix/transform as opendream_unimplemented
	//Vars that apply when a particle spawns
	var/lifespan as opendream_unimplemented
	var/fade as opendream_unimplemented
	var/fadein as opendream_unimplemented
	var/icon as opendream_unimplemented // not typed as an /icon because this can also be set to a list of icons (?????)
	var/icon_state as opendream_unimplemented // can also be a list
	var/color as opendream_unimplemented
	var/color_change as opendream_unimplemented
	var/position as opendream_unimplemented
	var/velocity as opendream_unimplemented
	var/scale as opendream_unimplemented
	var/grow as opendream_unimplemented // The DM ref says this is a num var, yet says its default value is list(0,0), when actually it is null.
	var/rotation as opendream_unimplemented
	var/spin as opendream_unimplemented
	var/friction as opendream_unimplemented
	//Vars that are evaluated every tick
	var/list/drift as opendream_unimplemented
