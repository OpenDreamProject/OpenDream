/var/world/world = /world as /world // Set to /world to suppress issues with Typemaker

//These procs should be in alphabetical order, as in DreamProcNativeRoot.cs
proc/alert(Usr = usr, Message, Title, Button1 = "Ok", Button2, Button3) as text
proc/animate(Object, time, loop, easing, flags) as null
proc/ascii2text(N) as text
proc/block(atom/Start, atom/End, StartZ, EndX=Start, EndY=End, EndZ=StartZ) as /list
proc/ceil(A) as num
proc/ckey(Key) as text|null
proc/ckeyEx(Text) as text|null
proc/clamp(Value, Low, High) as /list|num|null
proc/cmptext(T1) as num
proc/copytext(T, Start = 1, End = 0) as text|null
proc/copytext_char(T,Start=1,End=0) as text|null
proc/CRASH(msg) as null
proc/fcopy(Src, Dst) as num
proc/fcopy_rsc(File) as num|null
proc/fdel(File) as num
proc/fexists(File) as num
proc/file(Path)
proc/file2text(File) as text|null
proc/filter(type, ...)
proc/findtext(Haystack, Needle, Start = 1, End = 0) as num
proc/findtextEx(Haystack, Needle, Start = 1, End = 0) as num
proc/findlasttext(Haystack, Needle, Start = 1, End = 0) as num
proc/findlasttextEx(Haystack, Needle, Start = 1, End = 0) as num
proc/flick(Icon, Object)
proc/flist(Path) as /list
proc/floor(A) as num
proc/fract(n) as num
proc/ftime(File, IsCreationTime = 0) as num
proc/gradient(A, index)
proc/hascall(Object, ProcName) as num
proc/html_decode(HtmlText) as text
proc/html_encode(PlainText) as text
proc/icon_states(Icon, mode = 0) as text|null
proc/image(icon, loc, icon_state, layer, dir, pixel_x, pixel_y)
proc/isarea(Loc1) as num
proc/isfile(File) as num
proc/isicon(Icon) as num
proc/isinf(n) as num
proc/islist(Object) as num
proc/isloc(Loc1) as num
proc/ismob(Loc1) as num
proc/isobj(Loc1) as num
proc/ismovable(Loc1) as num
proc/isnan(n) as num
proc/isnull(Val) as num
proc/isnum(Val) as num
proc/ispath(Val, Type) as num
proc/istext(Val) as num
proc/isturf(Loc1) as num
proc/json_decode(JSON)
proc/json_encode(Value, flags)
proc/length_char(E) as num
proc/list2params(List) as text
proc/lowertext(T as text) as text
proc/max(A) as num|text|null
proc/md5(T) as text|null
proc/min(A) as num|text|null
proc/nonspantext(Haystack, Needles, Start = 1) as num
proc/num2text(N, A, B) as text
proc/orange(Dist = 5, Center = usr) as /list|null // NOTE: Not sure if return types have BYOND parity
proc/oview(Dist = 5, Center = usr) as /list
proc/oviewers(Depth = 5, Center = usr) as /list
proc/params2list(Params) as /list
proc/rand(L, H) as num
proc/rand_seed(Seed) as null
proc/range(Dist, Center) as /list|null // NOTE: Not sure if return types have BYOND parity
proc/ref(Object) as text
proc/replacetext(Haystack, Needle, Replacement, Start = 1, End = 0) as text|null
proc/replacetextEx(Haystack, Needle, Replacement, Start = 1, End = 0) as text|null
proc/rgb(R, G, B, A, space) as text|null
proc/rgb2num(color, space = COLORSPACE_RGB) as /list
proc/roll(ndice = 1, sides) as num
proc/round(A, B) as num
proc/sha1(input) as text|null
proc/shutdown(Addr,Natural = 0)
proc/sleep(Delay)
proc/sorttext(T1, T2) as num
proc/sorttextEx(T1, T2) as num
proc/sound(file, repeat = 0, wait, channel, volume)
proc/spantext(Haystack,Needles,Start=1) as num
proc/spantext_char(Haystack,Needles,Start=1) as num
proc/splicetext(Text, Start = 1, End = 0, Insert = "") as text|null
proc/splicetext_char(Text, Start = 1, End = 0, Insert = "") as text|null
proc/splittext(Text, Delimiter) as /list
proc/stat(Name, Value)
proc/statpanel(Panel, Name, Value)
proc/text2ascii(T, pos = 1) as text
proc/text2ascii_char(T, pos = 1) as text
proc/text2file(Text, File)
proc/text2num(T, radix = 10) as num|null
proc/text2path(T)
proc/time2text(timestamp, format) as text
proc/trimtext(Text) as text|null
proc/trunc(n) as num
proc/turn(Dir, Angle)
proc/typesof(Item1) as /list
proc/uppertext(T as text) as text
proc/url_decode(UrlText) as text
proc/url_encode(PlainText, format = 0) as text
proc/view(Dist = 5, Center = usr) as /list
proc/viewers(Depth, Center = usr) as /list
proc/walk(Ref, Dir, Lag = 0, Speed = 0)
proc/walk_to(Ref, Trg, Min = 0, Lag = 0, Speed = 0)
proc/walk_towards(Ref,Trg,Lag=0,Speed=0)
proc/winclone(player, window_name, clone_name)
proc/winexists(player, control_id) as text
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

proc/replacetextEx_char(Haystack as text, Needle, Replacement, Start = 1, End = 0) as text
	set opendream_unimplemented = TRUE
	return Haystack

/proc/step(atom/movable/Ref as /atom/movable, var/Dir, var/Speed=0) as num
	//TODO: Speed = step_size if Speed is 0
	return Ref.Move(get_step(Ref, Dir), Dir)

/proc/step_away(atom/movable/Ref as /atom/movable, /atom/Trg, Max=5, Speed=0) as num
    return Ref.Move(get_step_away(Ref, Trg, Max), turn(get_dir(Ref, Trg), 180))

/proc/step_to(atom/movable/Ref, atom/Trg, Min = 0, Speed = 0) as num
	//TODO: Consider obstacles

	var/dist = get_dist(Ref, Trg)
	if (dist <= Min) return

	var/step_dir = get_dir(Ref, Trg)
	return step(Ref, step_dir, Speed)

/proc/get_step_to(Ref, Trg, Min=0)
	set opendream_unimplemented = TRUE
	CRASH("/get_step_to() is not implemented")

/proc/get_steps_to(Ref, Trg, Min=0) as /list
	set opendream_unimplemented = TRUE
	CRASH("/get_steps_to() is not implemented")

/proc/walk_away(Ref,Trg,Max=5,Lag=0,Speed=0)
	set opendream_unimplemented = TRUE
	CRASH("/walk_away() is not implemented")

proc/get_dist(atom/Loc1, atom/Loc2) as num
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

proc/hearers(Depth = world.view, Center = usr) as /list
	set opendream_unimplemented = TRUE
	//TODO: Actual cursed hearers implementation
	return viewers(Depth, Center)

proc/ohearers(Depth = world.view, Center = usr) as /list
	set opendream_unimplemented = TRUE
	//TODO: Actual cursed ohearers implementation
	return oviewers(Depth, Center)

proc/step_towards(atom/movable/Ref as /atom/movable, /atom/Trg, Speed) as num
	return Ref.Move(get_step_towards(Ref, Trg), get_dir(Ref, Trg))

proc/step_rand(atom/movable/Ref, Speed=0)
	var/target = get_step_rand(Ref)
	return Ref.Move(target, get_dir(Ref, target))

proc/jointext(list/List as /list|text, Glue as text|null, Start = 1 as num, End = 0 as num) as text
	if(islist(List))
		return List.Join(Glue, Start, End)
	if(istext(List))
		return List
	CRASH("jointext was passed a non-list, non-text value")

proc/lentext(T) as num
	return length(T)

proc/winshow(player, window, show=1)
	winset(player, window, "is-visible=[show ? "true" : "false"]")

proc/refcount(var/Object) as num
	// woah that's a lot of refs
	// i wonder if it's true??
	return 100
	// (it's not)
