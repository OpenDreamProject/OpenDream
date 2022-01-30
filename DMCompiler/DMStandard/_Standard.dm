//The first global in the runtime is always `world`
//So keep this at the top
/var/world/world = null

proc/abs(A)
proc/alert(Usr = usr, Message, Title, Button1 = "Ok", Button2, Button3)
proc/animate(Object, time, loop, easing, flags)
proc/arccos(X)
proc/arcsin(X)
proc/arctan(A)
proc/ascii2text(N)
proc/ckey(Key)
proc/ckeyEx(Text)
proc/clamp(Value, Low, High)
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
proc/findlasttextEx(Haystack, Needle, Start = 1, End = 0)
proc/flick(Icon, Object)
proc/flist(Path)
proc/hascall(Object, ProcName)
proc/html_decode(HtmlText)
proc/html_encode(PlainText)
proc/icon_states(Icon, mode = 0)
proc/image(icon, loc, icon_state, layer, dir)
proc/isarea(Loc1)
proc/isfile(File)
proc/isicon(Icon)
proc/islist(Object)
proc/isloc(Loc1)
proc/ismob(Loc1)
proc/ismovable(Loc1)
proc/isnull(Val)
proc/isnum(Val)
proc/ispath(Val, Type)
proc/istext(Val)
proc/isturf(Loc1)
proc/json_decode(JSON)
proc/json_encode(Value)
proc/length(E)
proc/length_char(E)
proc/list2params(List)
proc/log(X, Y)
proc/lowertext(T)
proc/max(A)
proc/md5(T)
proc/min(A)
proc/nonspantext(Haystack, Needles, Start = 1)
proc/num2text(N, Digits, Radix)
proc/oview(Dist = 5, Center = usr)
proc/oviewers(Depth = 5, Center = usr)
proc/params2list(Params)
proc/prob(P)
proc/rand(L, H)
proc/rand_seed(Seed)
proc/ref(Object)
proc/replacetext(Haystack, Needle, Replacement, Start = 1, End = 0)
proc/replacetextEx(Haystack, Needle, Replacement, Start = 1, End = 0)
proc/rgb(R, G, B, A)
proc/rgb2num(color, space = COLORSPACE_RGB)
proc/roll(ndice = 1, sides)
proc/round(A, B)
proc/shutdown(Addr,Natural = 0)
proc/sin(X)
proc/sleep(Delay)
proc/sorttext(T1, T2)
proc/sorttextEx(T1, T2)
proc/sound(file, repeat = 0, wait, channel, volume)
proc/splittext(Text, Delimiter)
proc/sqrt(A)
proc/stat(Name, Value)
proc/statpanel(Panel, Name, Value)
proc/tan(X)
proc/text2ascii(T, pos = 1)
proc/text2file(Text, File)
proc/text2num(T, radix = 10)
proc/text2path(T)
proc/time2text(timestamp, format)
proc/typesof(Item1)
proc/uppertext(T)
proc/url_decode(UrlText)
proc/url_encode(PlainText, format = 0)
proc/view(Dist = 4, Center = usr)
proc/viewers(Depth, Center = usr)
proc/walk(Ref, Dir, Lag = 0, Speed = 0)
proc/walk_to(Ref, Trg, Min = 0, Lag = 0, Speed = 0)
proc/winset(player, control_id, params)

#include "Defines.dm"
#include "Types\Client.dm"
#include "Types\Datum.dm"
#include "Types\Exception.dm"
#include "Types\Icon.dm"
#include "Types\Image.dm"
#include "Types\List.dm"
#include "Types\Matrix.dm"
#include "Types\Mutable_Appearance.dm"
#include "Types\Regex.dm"
#include "Types\Savefile.dm"
#include "Types\Sound.dm"
#include "Types\World.dm"
#include "Types\Atoms\_Atom.dm"
#include "Types\Atoms\Area.dm"
#include "Types\Atoms\Mob.dm"
#include "Types\Atoms\Movable.dm"
#include "Types\Atoms\Obj.dm"
#include "Types\Atoms\Turf.dm"
#include "UnsortedAdditions.dm"

proc/block(var/atom/Start, var/atom/End)
	var/list/atoms = list()

	var/startX = min(Start.x, End.x)
	var/startY = min(Start.y, End.y)
	var/startZ = min(Start.z, End.z)
	var/endX = max(Start.x, End.x)
	var/endY = max(Start.y, End.y)
	var/endZ = max(Start.z, End.z)
	for (var/z=startZ; z<=endZ; z++)
		for (var/y=startY; y<=endY; y++)
			for (var/x=startX; x<=endX; x++)
				atoms.Add(locate(x, y, z))

	return atoms

// TODO: Investigate "for(var/turf/T in range(Dist, Center))"-style weirdness that BYOND does. It's a center-out spiral and we need to replicate that.
proc/range(Dist, Center)
	. = list()

	var/TrueDist
	var/atom/TrueCenter

	if(isnum(Dist))
		if(isnum(Center))
			. += Center
			return
		if(isnull(Center))
			TrueCenter = usr
			if(isnull(TrueCenter))
				return
		else
			TrueCenter = Center
		TrueDist = Dist
	else
		if(isnull(Center))
			var/atom/A = Dist
			if(istype(A))
				//TODO change this once spiralling is implemented
				TrueCenter = locate(1, 1, A.z)
				TrueDist = world.maxx > world.maxy ? world.maxx : world.maxy
			else
				return
		else
			if(!isnum(Center))
				CRASH("invalid view size")
			if(isnull(Dist))
				TrueCenter = usr
				if(isnull(TrueCenter))
					return
			else
				TrueCenter = Dist
			TrueDist = Center

	if(!istype(TrueCenter, /atom))
		. += TrueCenter
		return

	for (var/x = max(TrueCenter.x - TrueDist, 1); x <= min(TrueCenter.x + TrueDist, world.maxx); x++)
		for (var/y = max(TrueCenter.y - TrueDist, 1); y <= min(TrueCenter.y + TrueDist, world.maxy); y++)
			var/turf/t = locate(x, y, TrueCenter.z)

			if (t != null)
				. += t
				. += t.contents

proc/orange(Dist, Center)
	. = list()

	var/TrueDist
	var/atom/TrueCenter

	if(isnum(Dist))
		if(isnum(Center))
			. += Center
			return
		if(isnull(Center))
			TrueCenter = usr
			if(isnull(TrueCenter))
				return
		else
			TrueCenter = Center
		TrueDist = Dist
	else
		if(isnull(Center))
			var/atom/A = Dist
			if(istype(A))
				//TODO change this once spiralling is implemented
				TrueCenter = locate(1, 1, A.z)
				TrueDist = world.maxx > world.maxy ? world.maxx : world.maxy
			else
				return
		else
			if(!isnum(Center))
				CRASH("invalid view size")
			if(isnull(Dist))
				TrueCenter = usr
				if(isnull(TrueCenter))
					return
			else
				TrueCenter = Dist
			TrueDist = Center

	if(!istype(TrueCenter, /atom))
		. += TrueCenter
		return

	for (var/x = max(TrueCenter.x - TrueDist, 1); x <= min(TrueCenter.x + TrueDist, world.maxx); x++)
		for (var/y = max(TrueCenter.y - TrueDist, 1); y <= min(TrueCenter.y + TrueDist, world.maxy); y++)
			if (x == TrueCenter.x && y == TrueCenter.y) continue

			var/turf/t = locate(x, y, TrueCenter.z)
			if (t != null)
				. += t
				. += t.contents

proc/get_step(atom/Ref, Dir)
	if (Ref == null) return null

	var/x = Ref.x
	var/y = Ref.y
	var/z = Ref.z

	if (Dir & NORTH) y += 1
	else if (Dir & SOUTH) y -= 1

	if (Dir & EAST) x += 1
	else if (Dir & WEST) x -= 1

	if (Dir & UP) z += 1
	else if (Dir & DOWN) z -= 1

	return locate(x, y, z)

proc/get_dir(atom/Loc1, atom/Loc2)
	if (Loc1 == null || Loc2 == null || Loc1.z != Loc2.z) return 0

	var/dir = 0

	if (Loc2.x < Loc1.x) dir |= WEST
	else if (Loc2.x > Loc1.x) dir |= EAST

	if (Loc2.y < Loc1.y) dir |= SOUTH
	else if (Loc2.y > Loc1.y) dir |= NORTH

	return dir

/proc/step(atom/movable/Ref, var/Dir, var/Speed=0)
	//TODO: Speed = step_size if Speed is 0
	Ref.Move(get_step(Ref, Dir), Dir)

/proc/step_away(atom/movable/Ref, /atom/Trg, Max=5, Speed=0)
    Ref.Move(get_step_away(Ref, Trg, Max), turn(get_dir(Ref, Trg), 180))

/proc/step_to(atom/movable/Ref, atom/Trg, Min = 0, Speed = 0)
	//TODO: Consider obstacles

	var/dist = get_dist(Ref, Trg)
	if (dist <= Min) return

	var/step_dir = get_dir(Ref, Trg)
	step(Ref, step_dir, Speed)

/proc/walk_towards(Ref,Trg,Lag=0,Speed=0)
	set opendream_unimplemented = TRUE
	CRASH("/walk_towards() is not implemented")

/proc/get_step_to(Ref, Trg, Min=0)
	set opendream_unimplemented = TRUE
	CRASH("/get_step_to() is not implemented")

/proc/walk_away(Ref,Trg,Max=5,Lag=0,Speed=0)
	set opendream_unimplemented = TRUE
	CRASH("/walk_away() is not implemented")

/proc/turn(Dir, Angle)
	if (istype(Dir, /matrix))
		var/matrix/copy = new(Dir)
		return copy.Turn(Angle)

	var/dirAngle = 0

	switch (Dir)
		if (EAST) dirAngle = 0
		if (NORTHEAST) dirAngle = 45
		if (NORTH) dirAngle = 90
		if (NORTHWEST) dirAngle = 135
		if (WEST) dirAngle = 180
		if (SOUTHWEST) dirAngle = 225
		if (SOUTH) dirAngle = 270
		if (SOUTHEAST) dirAngle = 315
		else
			if (Angle != 0)
				return pick(NORTH, SOUTH, EAST, WEST)

	dirAngle += round(Angle, 45)
	if (dirAngle > 360) dirAngle -= 360
	else if (dirAngle < 0) dirAngle += 360

	switch (dirAngle)
		if (0, 360) return EAST
		if (45) return NORTHEAST
		if (90) return NORTH
		if (135) return NORTHWEST
		if (180) return WEST
		if (225) return SOUTHWEST
		if (270) return SOUTH
		if (315) return SOUTHEAST

proc/get_dist(atom/Loc1, atom/Loc2)
	if (Loc1 == Loc2) return -1

	var/distX = Loc2.x - Loc1.x
	var/distY = Loc2.y - Loc1.y
	return round(sqrt(distX ** 2 + distY ** 2))

proc/get_step_towards(atom/movable/Ref, /atom/Trg)
	var/dir = get_dir(Ref, Trg)

	return get_step(Ref, dir)

proc/get_step_away(atom/movable/Ref, /atom/Trg, Max = 5)
	var/dir = turn(get_dir(Ref, Trg), 180)

	return get_step(Ref, dir)

proc/get_step_rand(atom/movable/Ref)
	// BYOND's implementation seems to be heavily weighted in favor of Ref's dir.
	var/dir = pick(NORTH, SOUTH, EAST, WEST, NORTHEAST, SOUTHEAST, SOUTHWEST, NORTHWEST)

	return get_step(Ref, dir)

proc/hearers(Depth = world.view, Center = usr)
	//TODO: Actual cursed hearers implementation
	return viewers(Depth, Center)

proc/ohearers(Depth = world.view, Center = usr)
	return oviewers(Depth, Center)

proc/step_towards(atom/movable/Ref, /atom/Trg, Speed)
	Ref.Move(get_step_towards(Ref, Trg), get_dir(Ref, Trg))

proc/step_rand(atom/movable/Ref, Speed=0)
	var/target = get_step_rand(Ref)
	return Ref.Move(target, get_dir(Ref, target))

proc/jointext(list/List, Glue, Start = 1, End = 0)
	if (isnull(List)) CRASH("Invalid list")

	return List.Join(Glue, Start, End)

proc/lentext(T)
	return length(T)

proc/isobj(Loc1)
	for(var/arg in args)
		if (!istype(arg, /obj)) return 0

	return 1
