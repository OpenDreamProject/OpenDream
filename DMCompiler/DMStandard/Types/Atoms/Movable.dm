/atom/movable
	var/screen_loc

	var/animate_movement = FORWARD_STEPS as opendream_unimplemented
	var/list/locs = list() as opendream_unimplemented
	var/glide_size as opendream_unimplemented
	var/step_size as opendream_unimplemented
	var/bound_x as opendream_unimplemented
	var/bound_y as opendream_unimplemented
	var/bound_width as opendream_unimplemented
	var/bound_height as opendream_unimplemented

	//Undocumented var. "[x],[y]" or "[x],[y] to [x2],[y2]" based on bound_* vars
	var/bounds as opendream_unimplemented

	var/particles/particles as opendream_unimplemented

	proc/Bump(atom/Obstacle)

	proc/Move(atom/NewLoc, Dir=0)
		if (isnull(NewLoc)) return

		if (Dir != 0)
			dir = Dir

		if (loc == NewLoc || !loc.Exit(src, NewLoc)) return FALSE
		if (NewLoc.Enter(src, loc))
			var/atom/oldloc = loc
			var/area/oldarea = oldloc.loc
			var/area/newarea = NewLoc.loc
			loc = NewLoc

			oldloc.Exited(src, loc)
			loc.Entered(src, oldloc)
			if (newarea != oldarea)
				oldarea.Exited(src, loc)
				newarea.Entered(src, oldloc)

			return TRUE
		else
			return FALSE
