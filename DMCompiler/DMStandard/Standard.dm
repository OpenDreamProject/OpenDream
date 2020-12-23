/var/global/world/world = null

proc/abs(A)
proc/animate(Object, time, loop, easing, flags)
proc/ascii2text(N)
proc/ckey(Key)
proc/copytext(T, Start = 1, End = 0)
proc/CRASH(msg)
proc/fcopy(Src, Dst)
proc/fcopy_rsc(File)
proc/fdel(File)
proc/fexists(File)
proc/file(Path)
proc/file2text(File)
proc/findtext(Haystack, Needle, Start = 1, End = 0)
proc/findtextEx(Haystack, Needle, Start = 1, End = 0)
proc/findlasttext(Haystack, Needle, Start = 1, End = 0)
proc/get_dist(Loc1, Loc2)
proc/html_decode(HtmlText)
proc/html_encode(PlainText)
proc/image(icon, loc, icon_state, layer, dir)
proc/isarea(Loc1)
proc/isloc(Loc1)
proc/ismob(Loc1)
proc/isnull(Val)
proc/isnum(Val)
proc/ispath(Val, Type)
proc/istext(Val)
proc/isturf(Loc1)
proc/istype(Val, Type)
proc/json_decode(JSON)
proc/json_encode(Value)
proc/length(E)
proc/locate(X, Y, Z)
proc/log(X, Y)
proc/lowertext(T)
proc/max(A)
proc/min(A)
proc/num2text(N, Digits, Radix)
proc/orange(Dist, Center = usr)
proc/params2list(Params)
proc/pick(Val1)
proc/prob(P)
proc/rand(L, H)
proc/replacetext(Haystack, Needle, Replacement, Start = 1, End = 0)
proc/round(A, B)
proc/sleep(Delay)
proc/sound(file, repeat = 0, wait, channel, volume)
proc/splittext(Text, Delimiter)
proc/text(FormatText)
proc/text2ascii(T, pos = 1)
proc/text2file(Text, File)
proc/text2num(T, radix = 10)
proc/text2path(T)
proc/time2text(timestamp, format)
proc/typesof(Item1)
proc/uppertext(T)
proc/url_encode(PlainText, format = 0)
proc/view(Dist = 4, Center = usr)
proc/viewers(Depth, Center = usr)
proc/walk(Ref, Dir, Lag = 0, Speed = 0)
proc/walk_to(Ref, Trg, Min = 0, Lag = 0, Speed = 0)

/list
	var/len

	proc/Add(Item1)
	proc/Copy(Start = 1, End = 0)
	proc/Cut(Start = 1, End = 0)
	proc/Find(Elem, Start = 1, End = 0)
	proc/Insert(Index, Item1)
	proc/Join(Glue, Start = 1, End = 0)
	proc/Remove(Item1)
	proc/Swap(Index1, Index2)

/sound
	var/file = null
	var/repeat = 0
	var/wait = 0
	var/channel = 0
	var/volume = 100
	var/frequency = 0
	var/falloff = 1
	var/x
	var/y
	var/z

	proc/New(file, repeat=0, wait, channel, volume)
		src.file = file
		src.repeat = repeat
		if (wait != null) src.wait = wait
		if (channel != null) src.channel = channel
		if (volume != null) src.volume = volume

/mutable_appearance
	var/icon = null
	var/icon_state = ""
	var/color = "#FFFFFF"
	var/alpha = 255
	var/layer = 0.0
	var/pixel_x = 0
	var/pixel_y = 0

	proc/New(/mutable_appearance/appearance)
		if (istype(appearance, /mutable_appearance))
			src.icon = appearance.icon
			src.icon_state = appearance.icon_state
			src.color = appearance.color
			src.alpha = appearance.alpha
			src.layer = appearance.layer
			src.pixel_x = appearance.pixel_x
			src.pixel_y = appearance.pixel_y
		else if (!isnull(appearance))
			CRASH("Invalid arguments for /mutable_appearance/New()")

/client
	var/list/verbs = list()
	var/list/screen = list()
	var/list/images = list()
	var/mob/mob
	var/atom/eye
	var/key
	var/ckey

	proc/New(TopicData)
		mob = new world.mob(null)

		return mob

	proc/Topic(href, href_list, hsrc)
		if (hsrc != null)
			hsrc.Topic(href, href_list)

	proc/Click(object, location, control, params)
		object.Click(location, control, params)

	proc/Move(loc, dir)
		mob.Move(loc, dir)

	proc/North()
		Move(get_step(mob, 1), 1)

	proc/South()
		Move(get_step(mob, 2), 2)

	proc/East()
		Move(get_step(mob, 4), 4)

	proc/West()
		Move(get_step(mob, 8), 8)

/world
	var/list/contents = list()

	var/name = "OpenDream World"
	var/time
	var/timeofday
	var/realtime
	var/tick_lag = 0.5
	var/tick_usage

	var/mob/mob = /mob

	var/maxx = 0
	var/maxy = 0
	var/maxz = 0

	proc/New()
	proc/Del()

/datum
	var/type
	var/parent_type

	proc/New()
	proc/Del()
	proc/Topic(href, href_list)

/image
	parent_type = /datum

	var/icon = null
	var/icon_state = null
	var/loc = null
	var/layer = -1
	var/dir = 2
	var/pixel_x = 0
	var/pixel_y = 0
	var/color = "#FFFFFF"
	var/alpha = 255

	proc/New(icon, loc, icon_state, layer, dir)
		src.icon = icon
		if (!istext(loc))
			if (loc != null) src.loc = loc
			if (icon_state != null) src.icon_state = icon_state
			if (layer != null) src.layer = layer
			if (dir != null) src.dir = dir
		else
			if (loc != null) src.icon_state = loc
			if (icon_state != null) src.layer = icon_state
			if (layer != null) src.dir = layer

/atom
	parent_type = /datum

	var/list/contents = list()
	var/list/overlays = list()
	var/atom/loc
	var/dir = 2
	var/x = 0
	var/y = 0
	var/z = 0
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_z = 0
	var/pixel_w = 0

	var/icon = null
	var/icon_state = ""
	var/layer = 2.0
	var/alpha = 255
	var/color = "#FFFFFF"
	var/invisibility = 0

	var/gender = "neuter"
	var/density = 0

	proc/Click(location, control, params)

	proc/Entered(atom/movable/Obj, atom/OldLoc)
	proc/Exited(atom/movable/Obj, atom/newloc)
	proc/Uncrossed(atom/movable/O)
	proc/Crossed(atom/movable/O)

	proc/Cross(atom/movable/O)
		return !(src.density && O.density)

	proc/Enter(atom/movable/O, atom/oldloc)
		return 1

	proc/Exit(atom/movable/O, atom/newloc)
		return 1
	
	proc/Click(location, control, params)
		return

/atom/movable
	var/screen_loc

	proc/Move(NewLoc, Dir=0)
		loc = NewLoc

	proc/Bump(atom/Obstacle)
		return
	
	proc/Move(NewLoc, Dir=0)
		if (NewLoc.Enter(src, loc))
			loc = NewLoc
			if (Dir != 0)
				dir = Dir
		else
			var/atom/Obstacle = null

			if (!NewLoc.Cross(src))
				Obstacle = NewLoc
			else
				for (var/atom/content in NewLoc.contents)
					if (!content.Cross(src))
						Obstacle = content
						break
		
			Bump(Obstacle)

/area
	parent_type = /atom

	layer = 1.0

/turf
	parent_type = /atom

	layer = 2.0

	Enter(atom/movable/O, atom/oldloc)
		if (!src.Cross(O)) return 0

		for (var/atom/content in src.contents)
			if (!content.Cross(O)) return 0
		
		return 1

/obj
	parent_type = /atom/movable

	layer = 3.0

/mob
	parent_type = /atom/movable
	
	var/client/client
	var/key
	var/ckey

	layer = 4.0

	proc/Login()
	proc/Logout()

proc/block(var/atom/Start, var/atom/End)
	var/list/atoms = list()
	
	for (var/x=Start.x; x<End.x; x++)
		for (var/y=Start.y; y<End.y; y++)
			atoms.Add(locate(x, y, Start.z))
	
	return atoms

proc/get_step(atom/Ref, Dir)
	if (Ref == null) return null
	
	var/x = Ref.x
	var/y = Ref.y

	if (Dir & 1) y += 1
	else if (Dir & 2) y -= 1

	if (Dir & 4) x += 1
	else if (Dir & 8) x -= 1

	return locate(max(x, 1), max(y, 1), Ref.z)

proc/get_dir(atom/Loc1, atom/Loc2)
	var/loc1X = Loc1.x
	var/loc2X = Loc2.x
	var/loc1Y = Loc1.y
	var/loc2Y = Loc2.y

	if (loc2X < loc1X)
		if (loc2Y == loc1Y) return 8
		else if (loc2Y > loc1Y) return 9
		else return 10
	else if (loc2X > loc1X)
		if (loc2Y == loc1Y) return 4
		else if (loc2Y > loc1Y) return 5
		else return 6
	else if (loc2Y > loc1Y) return 1
	else return 2

/proc/step(atom/movable/Ref, var/Dir, var/Speed=0)
	Ref.Move(get_step(Ref, Dir), Dir)

/proc/turn(Dir, Angle)
	var/dirAngle = 0

	if (Dir == 1) dirAngle = 0
	else if (Dir == (1 | 4)) dirAngle = 45
	else if (Dir == 4) dirAngle = 90
	else if (Dir == (2 | 4)) dirAngle = 135
	else if (Dir == 2) dirAngle = 180
	else if (Dir == (2 | 8)) dirAngle = 225
	else if (Dir == 8) dirAngle = 270
	else if (Dir == (1 | 8)) dirAngle = 315
	else if (Angle != 0) return pick(1, 2, 4, 8)

	dirAngle += round(Angle, 45)
	if (dirAngle == 0 || dirAngle == 360) return 1
	else if (dirAngle == 45) return 1 | 4
	else if (dirAngle == 90) return 4
	else if (dirAngle == 135) return 2 | 4
	else if (dirAngle == 180) return 2
	else if (dirAngle == 225) return 2 | 8
	else if (dirAngle == 270) return 8
	else if (dirAngle == 315) return 1 | 8
