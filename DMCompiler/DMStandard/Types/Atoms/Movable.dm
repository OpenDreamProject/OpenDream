/atom/movable
	var/screen_loc

	var/animate_movement = FORWARD_STEPS as opendream_unimplemented

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