//The first global in the runtime is always `world`
//So keep this at the top
/var/world/world = null

//These procs should be in alphabetical order, as in DreamProcNativeRoot.cs
proc/abs(A) as num
proc/addtext(...) as text
proc/alert(Usr = usr, Message, Title, Button1 = "Ok", Button2, Button3) as text
proc/animate(Object, time, loop, easing, flags) as null
proc/arccos(X) as num
proc/arcsin(X) as num
proc/arctan(A) as num
proc/ascii2text(N) as text
proc/block(var/atom/Start, var/atom/End)
proc/ceil(A) as num
proc/ckey(Key) as text
proc/ckeyEx(Text) as text
proc/clamp(Value, Low, High) as num
proc/cmptext(T1) as num
proc/copytext(T, Start = 1, End = 0) as text
proc/copytext_char(T,Start=1,End=0) as text
proc/cos(X) as num
proc/CRASH(msg) as null
proc/fcopy(Src, Dst) as num
proc/fcopy_rsc(File)
proc/fdel(File) as num
proc/fexists(File) as num
proc/file(Path)
proc/file2text(File) as text
proc/filter(type, ...)
proc/findtext(Haystack, Needle, Start = 1, End = 0) as num
proc/findtextEx(Haystack, Needle, Start = 1, End = 0) as num
proc/findlasttext(Haystack, Needle, Start = 1, End = 0) as num
proc/findlasttextEx(Haystack, Needle, Start = 1, End = 0) as num
proc/flick(Icon, Object) as null
proc/flist(Path)
proc/floor(A) as num
proc/fract(n) as num
proc/ftime(File, IsCreationTime = 0)
proc/get_dir(atom/Loc1, atom/Loc2) as num
proc/get_step(atom/Ref, Dir) as turf
proc/gradient(A, index)
proc/hascall(Object, ProcName) as num
proc/html_decode(HtmlText) as text
proc/html_encode(PlainText) as text
proc/icon_states(Icon, mode = 0)
proc/image(icon, loc, icon_state, layer, dir, pixel_x, pixel_y)
proc/isarea(Loc1)
proc/isfile(File) as num
proc/isicon(Icon) as num
proc/isinf(n) as num
proc/islist(Object) as num
proc/isloc(Loc1) as num
proc/ismob(Loc1) as num
proc/ismovable(Loc1) as num
proc/isnan(n) as num
proc/isnull(Val) as num
proc/isnum(Val) as num
proc/ispath(Val, Type) as num
proc/istext(Val) as num
proc/isturf(Loc1) as num
proc/json_decode(JSON) as text
proc/json_encode(Value) as text
proc/length(E) as num
proc/length_char(E) as num
proc/list2params(List) as text
proc/log(X, Y) as num
proc/lowertext(T) as text
proc/max(A) as num
proc/md5(T) as text
proc/min(A) as num
proc/nonspantext(Haystack, Needles, Start = 1) as text
proc/num2text(N, Digits, Radix) as text
proc/orange(Dist = 5, Center = usr)
proc/oview(Dist = 5, Center = usr)
proc/oviewers(Depth = 5, Center = usr)
proc/params2list(Params)
proc/rand(L, H) as num
proc/rand_seed(Seed) as null
proc/range(Dist, Center)
proc/ref(Object) as text
proc/replacetext(Haystack, Needle, Replacement, Start = 1, End = 0) as text
proc/replacetextEx(Haystack, Needle, Replacement, Start = 1, End = 0) as text
proc/rgb(R, G, B, A) as text
proc/rgb2num(color, space = COLORSPACE_RGB)
proc/roll(ndice = 1, sides) as num
proc/round(A, B) as num
proc/sha1(input) as text
proc/shutdown(Addr,Natural = 0) as null
proc/sin(X) as num
proc/sleep(Delay) as null
proc/sorttext(T1, T2) as num
proc/sorttextEx(T1, T2) as num
proc/sound(file, repeat = 0, wait, channel, volume)
proc/spantext(Haystack,Needles,Start=1) as text
proc/spantext_char(Haystack,Needles,Start=1) as text
proc/splicetext(Text, Start = 1, End = 0, Insert = "") as text
proc/splicetext_char(Text, Start = 1, End = 0, Insert = "") as text
proc/splittext(Text, Delimiter)
proc/sqrt(A) as num
proc/stat(Name, Value) as null
proc/statpanel(Panel, Name, Value) as num
proc/tan(X) as num
proc/text2ascii(T, pos = 1) as text
proc/text2ascii_char(T, pos = 1) as text
proc/text2file(Text, File)
proc/text2num(T, radix = 10) as num
proc/text2path(T)
proc/time2text(timestamp, format) as text
proc/trimtext(Text) as text
proc/trunc(n) as num
proc/turn(Dir, Angle) as num
proc/typesof(Item1)
proc/uppertext(T) as text
proc/url_decode(UrlText) as text
proc/url_encode(PlainText, format = 0) as text
proc/view(Dist = 5, Center = usr)
proc/viewers(Depth, Center = usr)
proc/walk(Ref, Dir, Lag = 0, Speed = 0)
proc/walk_to(Ref, Trg, Min = 0, Lag = 0, Speed = 0)
proc/winclone(player, window_name, clone_name)
proc/winexists(player, control_id) as text
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

// TODO: Nothing below this line has had its return type declared

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

/proc/walk_towards(Ref,Trg,Lag=0,Speed=0)
	set opendream_unimplemented = TRUE
	CRASH("/walk_towards() is not implemented")

/proc/get_step_to(Ref, Trg, Min=0)
	set opendream_unimplemented = TRUE
	CRASH("/get_step_to() is not implemented")

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

proc/isobj(Loc1)
	for(var/arg in args)
		if (!istype(arg, /obj)) return 0

	return 1

proc/winshow(player, window, show=1)
	winset(player, window, "is-visible=[show ? "true" : "false"]")

proc/refcount(var/Object)
	// woah that's a lot of refs
	// i wonder if it's true??
	return 100
	// (it's not)
