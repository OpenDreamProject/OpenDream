/var/global/world/world = null

proc/abs(A)
proc/animate(Object, time, loop, easing, flags)
proc/arccos(X)
proc/arctan(A)
proc/ascii2text(N)
proc/ckey(Key)
proc/cmptext(T1)
proc/copytext(T, Start = 1, End = 0)
proc/cos(X)
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
proc/orange(Dist = 5, Center = usr)
proc/oview(Dist = 5, Center = usr)
proc/params2list(Params)
proc/pick(Val1)
proc/prob(P)
proc/rand(L, H)
proc/replacetext(Haystack, Needle, Replacement, Start = 1, End = 0)
proc/replacetextEx(Haystack, Needle, Replacement, Start = 1, End = 0)
proc/round(A, B)
proc/sin(X)
proc/sleep(Delay)
proc/sorttext(T1, T2)
proc/sorttextEx(T1, T2)
proc/sound(file, repeat = 0, wait, channel, volume)
proc/splittext(Text, Delimiter)
proc/sqrt(A)
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

	proc/New(Size)
		if (Size != null) len = Size

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

/client
	var/list/verbs = list()
	var/list/screen = list()
	var/list/images = list()

	var/mob/mob
	var/key
	var/ckey

	var/atom/eye
	var/view
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_z = 0
	var/pixel_w = 0

	proc/New(TopicData)
		view = world.view
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

	var/area = /area
	var/turf = /turf
	var/mob = /mob

	var/name = "OpenDream World"
	var/time
	var/timeofday
	var/realtime
	var/tick_lag = 1
	var/fps = 10
	var/tick_usage

	var/maxx = 0
	var/maxy = 0
	var/maxz = 0
	var/icon_size = 32
	var/view = 5

	proc/New()
	proc/Del()

/datum
	var/type
	var/parent_type

	var/tag = null

	proc/New()
	proc/Del()
	proc/Topic(href, href_list)

/matrix
	parent_type = /datum

	var/a = 1
	var/b = 0
	var/c = 0
	var/d = 0
	var/e = 1
	var/f = 0

	New(var/a = 1, var/b = 0, var/c = 0, var/d = 0, var/e = 1, var/f = 0)
		if (istype(a, /matrix))
			var/matrix/mat = a
			src.a = mat.a
			src.b = mat.b
			src.c = mat.c
			src.d = mat.d
			src.e = mat.e
			src.f = mat.f
		else
			src.a = a
			src.b = b
			src.c = c
			src.d = d
			src.e = e
			src.f = f

	proc/Turn(angle)
		var/angleCos = cos(angle)
		var/angleSin = sin(angle)

		a = a * angleCos + b * angleSin
		d = d * angleCos + e * angleSin
		e = a * -angleSin + e * angleCos
		b = d * -angleSin + b * angleCos

/mutable_appearance
	parent_type = /datum

	var/icon = null
	var/icon_state = ""
	var/color = "#FFFFFF"
	var/alpha = 255
	var/layer = 0.0
	var/pixel_x = 0
	var/pixel_y = 0

	New(mutable_appearance/appearance)
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
			if (dir != null) src.dir = dir

/atom
	parent_type = /datum

	var/name = "atom"
	var/desc = null

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
	var/plane = -32767
	var/alpha = 255
	var/color = "#FFFFFF"
	var/invisibility = 0
	var/matrix/transform

	var/gender = "neuter"
	var/density = 0

	proc/Click(location, control, params)

	proc/Entered(atom/movable/Obj, atom/OldLoc)
	proc/Exited(atom/movable/Obj, atom/newloc)
	proc/Uncrossed(atom/movable/O)
	proc/Crossed(atom/movable/O)

	proc/Cross(atom/movable/O)
		return !(src.density && O.density)

	proc/Uncross(atom/movable/O)
		return 1

	proc/Enter(atom/movable/O, atom/oldloc)
		return 1

	proc/Exit(atom/movable/O, atom/newloc)
		return 1
	
	proc/Click(location, control, params)
		return

/atom/movable
	var/screen_loc

	proc/Bump(atom/Obstacle)
	
	proc/Move(NewLoc, Dir=0)
		if (Dir != 0)
				dir = Dir

		if (loc == NewLoc || !loc.Exit(src, NewLoc)) return 0
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

			return 1
		else
			return 0

/area
	parent_type = /atom

	layer = 1.0

/turf
	parent_type = /atom

	layer = 2.0

	Enter(atom/movable/O, atom/oldloc)
		if (!src.Cross(O)) return 0

		for (var/atom/content in src.contents)
			if (!content.Cross(O))
				O.Bump(content)
				return 0
		
		return 1

	Exit(atom/movable/O, atom/newloc)
		if (!src.Uncross(O)) return 0

		for(var/atom/content in src.contents)
			if (content != O && !content.Uncross(O)) return 0

		return 1

	Entered(atom/movable/Obj, atom/OldLoc)
		for (var/atom/crossed in src)
			crossed.Crossed(Obj)

	Exited(atom/movable/Obj, atom/newloc)
		for (var/atom/uncrossed in src)
			uncrossed.Uncrossed(Obj)

/obj
	parent_type = /atom/movable

	layer = 3.0

/mob
	parent_type = /atom/movable
	
	var/client/client
	var/key
	var/ckey

	var/see_invisible = 0

	layer = 4.0

	proc/Login()
	proc/Logout()

proc/matrix(var/a, var/b, var/c, var/d, var/e, var/f)
	return new /matrix(a, b, c, d, e, f)

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
	if (Loc1 == null || Loc2 == null) return 0

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
	if (dirAngle > 360) dirAngle -= 360
	else if (dirAngle < 0) dirAngle += 360

	if (dirAngle == 0 || dirAngle == 360) return 1
	else if (dirAngle == 45) return 1 | 4
	else if (dirAngle == 90) return 4
	else if (dirAngle == 135) return 2 | 4
	else if (dirAngle == 180) return 2
	else if (dirAngle == 225) return 2 | 8
	else if (dirAngle == 270) return 8
	else if (dirAngle == 315) return 1 | 8

proc/step_towards(atom/movable/Ref, /atom/Trg, Speed)
	var/dir = get_dir(Ref, Trg)

	Ref.Move(get_step(Ref, dir), dir)