/turf
	parent_type = /atom

	layer = TURF_LAYER

	Enter(atom/movable/O, atom/oldloc)
		if (!src.Cross(O)) return FALSE

		for (var/atom/content in src.contents)
			if (!content.Cross(O))
				O.Bump(content)
				return FALSE

		return TRUE

	Exit(atom/movable/O, atom/newloc)
		if (!src.Uncross(O)) return FALSE

		for(var/atom/content in src.contents)
			if (content != O && !content.Uncross(O)) return FALSE

		return 1

	Entered(atom/movable/Obj, atom/OldLoc)
		Crossed(Obj)
		// /atom/movable/Move() is responsible for calling Crossed() on contents

	Exited(atom/movable/Obj, atom/newloc)
		Uncrossed(Obj)
		// /atom/movable/Move() is responsible for calling Uncrossed() on contents
