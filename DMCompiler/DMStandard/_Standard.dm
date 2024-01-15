/var/world/world = null

//These procs should be in alphabetical order, as in DreamProcNativeRoot.cs
proc/addtext(...)
proc/alert(Usr = usr, Message, Title, Button1 = "Ok", Button2, Button3)
proc/animate(Object, time, loop, easing, flags)
proc/ascii2text(N)
proc/block(atom/Start, atom/End, StartZ, EndX=Start, EndY=End, EndZ=StartZ)
proc/ceil(A)
proc/ckey(Key)
proc/ckeyEx(Text)
proc/clamp(Value, Low, High)
proc/cmptext(T1)
proc/copytext(T, Start = 1, End = 0)
proc/copytext_char(T,Start=1,End=0)
proc/CRASH(msg)
proc/fcopy(Src, Dst)
proc/fcopy_rsc(File)
proc/fdel(File)
proc/fexists(File)
proc/file(Path)
proc/file2text(File)
proc/filter(type, ...)
proc/findtext(Haystack, Needle, Start = 1, End = 0)
proc/findtextEx(Haystack, Needle, Start = 1, End = 0)
proc/findlasttext(Haystack, Needle, Start = 1, End = 0)
proc/findlasttextEx(Haystack, Needle, Start = 1, End = 0)
proc/flick(Icon, Object)
proc/flist(Path)
proc/floor(A)
proc/fract(n)
proc/ftime(File, IsCreationTime = 0)
proc/get_dir(atom/Loc1, atom/Loc2)
proc/get_step(atom/Ref, Dir)
proc/gradient(A, index)
proc/hascall(Object, ProcName)
proc/html_decode(HtmlText)
proc/html_encode(PlainText)
proc/icon_states(Icon, mode = 0)
proc/image(icon, loc, icon_state, layer, dir, pixel_x, pixel_y)
proc/isarea(Loc1)
proc/isfile(File)
proc/isicon(Icon)
proc/isinf(n)
proc/islist(Object)
proc/isloc(Loc1)
proc/ismob(Loc1)
proc/isobj(Loc1)
proc/ismovable(Loc1)
proc/isnan(n)
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
proc/lowertext(T)
proc/max(A)
proc/md5(T)
proc/min(A)
proc/nonspantext(Haystack, Needles, Start = 1)
proc/num2text(N, A, B)
proc/orange(Dist = 5, Center = usr)
proc/oview(Dist = 5, Center = usr)
proc/oviewers(Depth = 5, Center = usr)
proc/params2list(Params)
proc/rand(L, H)
proc/rand_seed(Seed)
proc/range(Dist, Center)
proc/ref(Object)
proc/replacetext(Haystack, Needle, Replacement, Start = 1, End = 0)
proc/replacetextEx(Haystack, Needle, Replacement, Start = 1, End = 0)
proc/rgb(R, G, B, A)
proc/rgb2num(color, space = COLORSPACE_RGB)
proc/roll(ndice = 1, sides)
proc/round(A, B)
proc/sha1(input)
proc/shutdown(Addr,Natural = 0)
proc/sleep(Delay)
proc/sorttext(T1, T2)
proc/sorttextEx(T1, T2)
proc/sound(file, repeat = 0, wait, channel, volume)
proc/spantext(Haystack,Needles,Start=1)
proc/spantext_char(Haystack,Needles,Start=1)
proc/splicetext(Text, Start = 1, End = 0, Insert = "")
proc/splicetext_char(Text, Start = 1, End = 0, Insert = "")
proc/splittext(Text, Delimiter)
proc/stat(Name, Value)
proc/statpanel(Panel, Name, Value)
proc/text2ascii(T, pos = 1)
proc/text2ascii_char(T, pos = 1)
proc/text2file(Text, File)
proc/text2num(T, radix = 10)
proc/text2path(T)
proc/time2text(timestamp, format)
proc/trimtext(Text)
proc/trunc(n)
proc/turn(Dir, Angle)
proc/typesof(Item1)
proc/uppertext(T)
proc/url_decode(UrlText)
proc/url_encode(PlainText, format = 0)
proc/view(Dist = 5, Center = usr)
proc/viewers(Depth, Center = usr)
proc/walk(Ref, Dir, Lag = 0, Speed = 0)
proc/walk_to(Ref, Trg, Min = 0, Lag = 0, Speed = 0)
proc/walk_towards(Ref,Trg,Lag=0,Speed=0)
proc/winclone(player, window_name, clone_name)
proc/winexists(player, control_id)
proc/winget(player, control_id, params)
proc/winset(player, control_id, params)

#include "Defines.dm"
#include "Types\Client.dm"
#include "Types\Datum.dm"
#include "Types\Exception.dm"
#include "Types\Filter.dm"
#include "Types\Generator.dm"
#include "Types\Icon.dm"
#include "Types\Image.dm"
#include "Types\List.dm"
#include "Types\Matrix.dm"
#include "Types\Mutable_Appearance.dm"
#include "Types\Particles.dm"
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

proc/replacetextEx_char(Haystack, Needle, Replacement, Start = 1, End = 0)
	set opendream_unimplemented = TRUE
	return Haystack

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

/proc/get_step_to(Ref, Trg, Min=0)
	set opendream_unimplemented = TRUE
	CRASH("/get_step_to() is not implemented")

/proc/get_steps_to(Ref, Trg, Min=0)
	set opendream_unimplemented = TRUE
	CRASH("/get_steps_to() is not implemented")

/proc/walk_away(Ref,Trg,Max=5,Lag=0,Speed=0)
	set opendream_unimplemented = TRUE
	CRASH("/walk_away() is not implemented")

proc/get_dist(atom/Loc1, atom/Loc2)
	if (!istype(Loc1) || !istype(Loc2)) return 127
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
	set opendream_unimplemented = TRUE
	//TODO: Actual cursed hearers implementation
	return viewers(Depth, Center)

proc/ohearers(Depth = world.view, Center = usr)
	set opendream_unimplemented = TRUE
	//TODO: Actual cursed ohearers implementation
	return oviewers(Depth, Center)

proc/step_towards(atom/movable/Ref, /atom/Trg, Speed)
	Ref.Move(get_step_towards(Ref, Trg), get_dir(Ref, Trg))

proc/step_rand(atom/movable/Ref, Speed=0)
	var/target = get_step_rand(Ref)
	return Ref.Move(target, get_dir(Ref, target))

proc/jointext(list/List, Glue, Start = 1, End = 0)
	if(islist(List))
		return List.Join(Glue, Start, End)
	if(istext(List))
		return List
	CRASH("jointext was passed a non-list, non-text value")

proc/lentext(T)
	return length(T)

proc/winshow(player, window, show=1)
	winset(player, window, "is-visible=[show ? "true" : "false"]")

proc/refcount(var/Object)
	// woah that's a lot of refs
	// i wonder if it's true??
	return 100
	// (it's not)
