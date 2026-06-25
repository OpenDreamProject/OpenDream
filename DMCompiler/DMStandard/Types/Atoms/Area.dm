/area
	parent_type = /atom

	layer = 1.0
	luminosity = 1

	Enter(atom/movable/O, atom/oldloc)
		if(!..())
			return FALSE
		return src.Cross(O)

	Entered(atom/movable/Obj, atom/OldLoc)
		..()
		Crossed(Obj)

	Exit(atom/movable/O, atom/newloc)
		if (!..())
			return FALSE
		return src.Uncross(O)

	Exited(atom/movable/Obj, atom/newloc)
		..()
		Uncrossed(Obj)