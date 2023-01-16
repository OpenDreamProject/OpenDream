/var/world/world = null
proc/abs(A)
proc/addtext(...)
proc/alert(Usr = usr, Message, Title, Button1 = "Ok", Button2, Button3)
proc/animate(Object, time, loop, easing, flags)
proc/arccos(X)
proc/arcsin(X)
proc/arctan(A)
proc/ascii2text(N)
proc/ceil(A)
proc/ckey(Key)
proc/ckeyEx(Text)
proc/clamp(Value, Low, High)
proc/cmptext(T1)
proc/copytext(T, Start = 1, End = 0)
proc/copytext_char(T,Start=1,End=0)
proc/cos(X)
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
proc/rand(L, H)
proc/rand_seed(Seed)
proc/ref(Object)
proc/replacetext(Haystack, Needle, Replacement, Start = 1, End = 0)
proc/replacetextEx(Haystack, Needle, Replacement, Start = 1, End = 0)
proc/rgb(R, G, B, A)
proc/rgb2num(color, space = COLORSPACE_RGB)
proc/roll(ndice = 1, sides)
proc/round(A, B)
proc/sha1(input)
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
proc/trimtext(Text)
proc/trunc(n)
proc/typesof(Item1)
proc/uppertext(T)
proc/url_decode(UrlText)
proc/url_encode(PlainText, format = 0)
proc/view(Dist = 4, Center = usr)
proc/viewers(Depth, Center = usr)
proc/walk(Ref, Dir, Lag = 0, Speed = 0)
proc/walk_to(Ref, Trg, Min = 0, Lag = 0, Speed = 0)
proc/winexists(player, control_id)
proc/winset(player, control_id, params)
/client
	var/list/verbs = list()
	var/list/screen = list()
	var/list/images = list() as opendream_unimplemented
	var/list/vars
	var/atom/statobj
	var/statpanel
	var/default_verb_category = "Commands"
	var/tag
	var/type = /client
	var/mob/mob
	var/atom/eye
	var/perspective = 0
	var/view
	var/pixel_x = 0 as opendream_unimplemented
	var/pixel_y = 0 as opendream_unimplemented
	var/pixel_z = 0 as opendream_unimplemented
	var/pixel_w = 0 as opendream_unimplemented
	var/show_popup_menus = 1 as opendream_unimplemented
	var/show_verb_panel = 1 as opendream_unimplemented
	var/byond_version = 514
	var/byond_build = 1584
	var/address
	var/inactivity = 0 as opendream_unimplemented
	var/key
	var/ckey
	var/connection
	var/computer_id = 0
	var/tick_lag = 0 as opendream_unimplemented
	var/timezone
	var/script as opendream_unimplemented
	var/color = 0 as opendream_unimplemented
	var/control_freak as opendream_unimplemented
	var/mouse_pointer_icon as opendream_unimplemented
	var/preload_rsc = 1 as opendream_unimplemented
	var/fps = 0 as opendream_unimplemented
	var/dir = 1 as opendream_unimplemented
	var/gender = "neuter" as opendream_unimplemented
	var/glide_size as opendream_unimplemented
	var/virtual_eye as opendream_unimplemented
	proc/New(TopicData)
		view = world.view
		mob = new world.mob(null)
		return mob
	proc/Del()
		set opendream_unimplemented = 1
	proc/Topic(href, list/href_list, datum/hsrc)
		if (hsrc != null)
			hsrc.Topic(href, href_list)
	proc/Stat()
		if (statobj != null) statobj.Stat()
	proc/Command(command as command_text)
		set opendream_unimplemented = 1
	proc/Import(Query)
		set opendream_unimplemented = 1
	proc/Export(file)
		set opendream_unimplemented = 1
	proc/AllowUpload(filename, filelength)
		set opendream_unimplemented = 1
		return 1
	proc/SoundQuery()
		set opendream_unimplemented = 1
	proc/MeasureText(text, style, width=0)
		set opendream_unimplemented = 1
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
	proc/Northeast()
		Move(get_step(mob, 5), 5)
	proc/Southeast()
		Move(get_step(mob, 6), 6)
	proc/Southwest()
		Move(get_step(mob, 10), 10)
	proc/Northwest()
		Move(get_step(mob, 9), 9)
	proc/Center()
	proc/Click(atom/object, location, control, params)
		object.Click(location, control, params)
	proc/DblClick(atom/object, location, control, params)
		set opendream_unimplemented = 1
		object.DblClick(location,control,params)
	proc/MouseDown(atom/object, location, control, params)
		set opendream_unimplemented = 1
		object.MouseDown(location, control, params)
	proc/MouseDrag(atom/src_object,over_object,src_location,over_location,src_control,over_control,params)
		set opendream_unimplemented = 1
		src_object.MouseDrag(over_object,src_location,over_location,src_control,over_control,params)
	proc/MouseDrop(atom/src_object,over_object,src_location,over_location,src_control,over_control,params)
		set opendream_unimplemented = 1
		src_object.MouseDrop(over_object,src_location,over_location,src_control,over_control,params)
	proc/MouseEntered(atom/object,location,control,params)
		set opendream_unimplemented = 1
		object.MouseEntered(location,control,params)
	proc/MouseExited(atom/object,location,control,params)
		set opendream_unimplemented = 1
		object.MouseExited(location,control,params)
	proc/MouseMove(atom/object,location,control,params)
		set opendream_unimplemented = 1
		object.MouseMove(location,control,params)
	proc/MouseUp(atom/object,location,control,params)
		set opendream_unimplemented = 1
		object.MouseUp(location,control,params)
	proc/MouseWheel(atom/object,delta_x,delta_y,location,control,params)
		set opendream_unimplemented = 1
		object.MouseWheel(delta_x,delta_y,location,control,params)
	proc/IsByondMember()
		set opendream_unimplemented = 1
		return 0
	proc/CheckPassport(passport_identifier)
		set opendream_unimplemented = 1
	proc/SendPage(msg, recipient, options)
		set opendream_unimplemented = 1
	proc/GetAPI(Api, Name)
		set opendream_unimplemented = 1
	proc/SetAPI(Api, Key, Value)
		set opendream_unimplemented = 1
/datum
	var/type
	var/parent_type
	var/list/vars
	var/tag
	proc/New()
	proc/Del()
	proc/Topic(href, href_list)
/exception
	parent_type = /datum
	var/name
	var/desc
	var/file
	var/line
/exception/New(N, F, L)
	name = N
	file = F
	line = L
/dm_filter
	var/x
	var/y
	var/icon
	var/render_source
	var/flags
	var/size
	var/threshold
	var/offset
	var/alpha
	var/color
	var/space
	var/transform
	var/blend_mode
	var/factor
	var/density
	var/repeat
	var/radius
	var/falloff
/generator
	parent_type = /datum
	var/_binobj as opendream_unimplemented
/generator/proc/Rand()
	set opendream_unimplemented = 1
/icon
	parent_type = /datum
	var/icon
	New(icon, icon_state, dir, frame, moving)
	proc/Blend(icon, function = 0, x = 1, y = 1)
	proc/Crop(x1, y1, x2, y2)
		set opendream_unimplemented = 1
	proc/DrawBox(rgb, x1, y1, x2 = x1, y2 = y1)
		set opendream_unimplemented = 1
	proc/Flip(dir)
		set opendream_unimplemented = 1
	proc/GetPixel(x, y, icon_state, dir = 0, frame = 0, moving = -1)
		set opendream_unimplemented = 1
	proc/Height()
	proc/IconStates(mode = 0)
		return icon_states(src, mode)
	proc/Insert(new_icon, icon_state, dir, frame, moving, delay)
	proc/MapColors(...)
		set opendream_unimplemented = 1
	proc/Scale(width, height)
	proc/SetIntensity(r, g = r, b = r)
		set opendream_unimplemented = 1
	proc/Shift(dir, offset, wrap = 0)
		set opendream_unimplemented = 1
	proc/SwapColor(old_rgb, new_rgb)
		set opendream_unimplemented = 1
	proc/Turn(angle)
		set opendream_unimplemented = 1
	proc/Width()
proc/icon(icon, icon_state, dir, frame, moving)
	return new /icon(icon, icon_state, dir, frame, moving)
/image
	parent_type = /datum
	var/alpha = 255
	var/appearance as opendream_unimplemented
	var/appearance_flags = 0 as opendream_unimplemented
	var/blend_mode = 0 as opendream_unimplemented
	var/color = "#FFFFFF"
	var/desc = null
	var/gender = "neuter" as opendream_unimplemented
	var/infra_luminosity = 0 as opendream_unimplemented
	var/invisibility = 0 as opendream_unimplemented
	var/list/filters = list()
	var/layer = -1
	var/luminosity = 0 as opendream_unimplemented
	var/maptext = "i" as opendream_unimplemented
	var/maptext_width = 32 as opendream_unimplemented
	var/maptext_height = 32 as opendream_unimplemented
	var/maptext_x = 0 as opendream_unimplemented
	var/maptext_y = 0 as opendream_unimplemented
	var/mouse_over_pointer = 0 as opendream_unimplemented
	var/mouse_drag_pointer = 0 as opendream_unimplemented
	var/mouse_drop_pointer = 1 as opendream_unimplemented
	var/mouse_drop_zone = 0 as opendream_unimplemented
	var/mouse_opacity = 1
	var/name = "image"
	var/opacity = 0 as opendream_unimplemented
	var/list/overlays = list()
	var/override = 1 as opendream_unimplemented
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_w = 0 as opendream_unimplemented
	var/pixel_z = 0 as opendream_unimplemented
	var/plane = -32767 as opendream_unimplemented
	var/render_source as opendream_unimplemented
	var/render_target as opendream_unimplemented
	var/suffix as opendream_unimplemented
	var/text = "i" as opendream_unimplemented
	var/matrix/transform
	var/list/underlays = list()
	var/vis_flags = 0 as opendream_unimplemented
	var/bound_width as opendream_unimplemented
	var/bound_height as opendream_unimplemented
	var/x
	var/y
	var/z
	var/list/vis_contents = list() as opendream_unimplemented
	var/dir = 2
	var/icon
	var/icon_state
	var/atom/loc = null
	New(icon, loc, icon_state, layer, dir, pixel_x, pixel_y)
		src.icon = icon
		if (!istext(loc))
			if (loc != null) src.loc = loc
			if (icon_state != null) src.icon_state = icon_state
			if (layer != null) src.layer = layer
			if (dir != null) src.dir = dir
			if (pixel_x != null) src.pixel_x = pixel_x
			if (pixel_y != null) src.pixel_y = pixel_y
		else
			if (loc != null) src.icon_state = loc
			if (icon_state != null) src.layer = icon_state
			if (layer != null) src.dir = layer
			if (dir != null) src.pixel_x = dir
			if (pixel_x != null) src.pixel_y = pixel_x
/list
	var/len
	var/type = /list
	proc/New(Size)
	proc/Add(Item1)
	proc/Copy(Start = 1, End = 0)
	proc/Cut(Start = 1, End = 0)
	proc/Find(Elem, Start = 1, End = 0)
	proc/Insert(Index, Item1)
	proc/Remove(Item1)
	proc/Swap(Index1, Index2)
	proc/Splice(Start=1,End=0, ...)
		set opendream_unimplemented = 1
	proc/Join(Glue, Start = 1, End = 0)
		if (End == 0) End = src.len
		var/result = ""
		for (var/i in Start to End)
			result += "[src[i]][(i != End) ? Glue : null]"
		return result
/matrix
	parent_type = /datum
	var/a = 1
	var/b = 0
	var/c = 0
	var/d = 0
	var/e = 1
	var/f = 0
	proc/Interpolate(Matrix2, t)
		set opendream_unimplemented = 1
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
	proc/Add(matrix/Matrix2)
		if(!istype(Matrix2))
			CRASH("Invalid matrix")
		a += Matrix2.a
		b += Matrix2.b
		c += Matrix2.c
		d += Matrix2.d
		e += Matrix2.e
		f += Matrix2.f
		return src
	proc/Invert()
		var/determinant = a*e - d*b
		if(!determinant)
			CRASH("Invalid matrix")
		var/old_a = a
		var/old_b = b
		var/old_c = c
		var/old_d = d
		var/old_e = e
		var/old_f = f
		a = old_e
		b = -old_b
		c = old_b*old_f - old_e*old_c
		d = -old_d
		e = old_a
		f = old_d*old_c - old_a*old_f
		return Scale(1/determinant)
	proc/Multiply(m)
		if(!istype(m, /matrix))
			return Scale(m)
		var/matrix/n = m
		var/old_a = a
		var/old_b = b
		var/old_c = c
		var/old_d = d
		var/old_e = e
		var/old_f = f
		a = old_a*n.a + old_d*n.b
		b = old_b*n.a + old_e*n.b
		c = old_c*n.a + old_f*n.b + n.c
		d = old_a*n.d + old_d*n.e
		e = old_b*n.d + old_e*n.e
		f = old_c*n.d + old_f*n.e + n.f
		return src
	proc/Scale(x, y)
		if(!isnum(x))
			x = 0
		if(!isnum(y))
			y = x
		a = a * x
		b = b * x
		c = c * x
		d = d * y
		e = e * y
		f = f * y
		return src
	proc/Subtract(matrix/Matrix2)
		if(!istype(Matrix2))
			CRASH("Invalid matrix")
		a -= Matrix2.a
		b -= Matrix2.b
		c -= Matrix2.c
		d -= Matrix2.d
		e -= Matrix2.e
		f -= Matrix2.f
		return src
	proc/Translate(x, y = x)
		c += x
		f += y
	proc/Turn(angle)
		var/angleCos = cos(angle)
		var/angleSin = sin(angle)
		var/matrix/rotation = new(angleCos, angleSin, 0, -angleSin, angleCos, 0)
		return Multiply(rotation)
proc/matrix(var/a, var/b, var/c, var/d, var/e, var/f)
	return new /matrix(a, b, c, d, e, f)
/mutable_appearance
	parent_type = /image
	var/animate_movement = 1 as opendream_unimplemented
	var/screen_loc as opendream_unimplemented
	New(var/datum/copy_from)
		if (istype(copy_from, /mutable_appearance))
			var/mutable_appearance/appearance = copy_from
			src.icon = appearance.icon
			src.icon_state = appearance.icon_state
			src.dir = appearance.dir
			src.color = appearance.color
			src.alpha = appearance.alpha
			src.layer = appearance.layer
			src.pixel_x = appearance.pixel_x
			src.pixel_y = appearance.pixel_y
		else if (istype(copy_from, /image))
			var/image/image = copy_from
			src.icon = image.icon
			src.icon_state = image.icon_state
			src.dir = image.dir
			src.color = image.color
			src.alpha = image.alpha
			src.layer = image.layer
			src.pixel_x = image.pixel_x
			src.pixel_y = image.pixel_y
		else if (isfile(copy_from))
			src.icon = copy_from
		else if (!isnull(copy_from))
			CRASH("Invalid arguments for /mutable_appearance/New()")
/particles
	parent_type = /datum
	var/width = 100 as opendream_unimplemented 
	var/height = 100 as opendream_unimplemented 
	var/count = 100 as opendream_unimplemented 
	var/spawning = 1 as opendream_unimplemented 
	var/bound1 = -1000 as opendream_unimplemented 
	var/bound2 = 1000 as opendream_unimplemented 
	var/gravity as opendream_unimplemented 
	var/list/gradient = null as opendream_unimplemented 
	var/transform as opendream_unimplemented 
	var/lifespan as opendream_unimplemented  
	var/fade as opendream_unimplemented 
	var/fadein as opendream_unimplemented 
	var/icon as opendream_unimplemented 
	var/icon_state as opendream_unimplemented 
	var/color as opendream_unimplemented 
	var/color_change as opendream_unimplemented 
	var/position as opendream_unimplemented 
	var/velocity as opendream_unimplemented 
	var/scale as opendream_unimplemented 
	var/grow as opendream_unimplemented 
	var/rotation as opendream_unimplemented 
	var/spin as opendream_unimplemented 
	var/friction as opendream_unimplemented 
	var/drift as opendream_unimplemented 
/regex
	parent_type = /datum
	var/flags
	var/list/group
	var/index
	var/match
	var/name
	var/next
	var/text
	New(pattern, flags)
		if (istype(pattern, /regex))
			var/regex/Regex = pattern
			src.name = Regex.name
			src.flags = Regex.flags
		else
			src.name = pattern
			src.flags = flags
	proc/Find(haystack, Start = 1, End = 0)
	proc/Replace(haystack, replacement, Start = 1, End = 0)
	proc/Replace_char(haystack, replacement, Start = 1, End = 0)
		set opendream_unimplemented = 1
		return haystack
proc/regex(pattern, flags)
/savefile
	var/cd
	var/list/dir
	var/eof
	var/name
	proc/New(filename, timeout)
	proc/Flush()
	proc/ExportText(path = cd, file)
	proc/ImportText(path = cd, source)
		set opendream_unimplemented = 1
	proc/Lock(timeout)
		set opendream_unimplemented = 1
	proc/Unlock()
		set opendream_unimplemented = 1
/sound
	parent_type = /datum
	var/file = null
	var/repeat = 0 as opendream_unimplemented
	var/wait = 0
	var/channel = 0
	var/volume = 100
	var/frequency = 0
	var/pan = 0 as opendream_unimplemented
	var/falloff = 1 as opendream_unimplemented
	var/x as opendream_unimplemented
	var/y as opendream_unimplemented
	var/z as opendream_unimplemented
	var/environment as opendream_unimplemented
	var/echo as opendream_unimplemented
	var/len as opendream_unimplemented
	var/offset as opendream_unimplemented
	var/priority = 0 as opendream_unimplemented
	var/status = 0 as opendream_unimplemented
	New(file, repeat=0, wait, channel, volume)
		if (istype(file, /sound))
			var/sound/copy_from = file
			src.file = copy_from.file
			src.wait = copy_from.wait
			src.channel = copy_from.channel
			src.volume = copy_from.volume
		else
			if(file != null)
				src.file = file
			if (wait != null) src.wait = wait
			if (channel != null) src.channel = channel
			if (volume != null) src.volume = volume
/world
	var/list/contents = list()
	var/list/vars
	var/log = null
	var/area = /area
	var/turf = /turf
	var/mob = /mob
	var/name = "OpenDream World"
	var/time
	var/timeofday
	var/realtime
	var/tick_lag = 1
	var/cpu = 0 as opendream_unimplemented
	var/fps = 10
	var/tick_usage
	var/loop_checks = 0 as opendream_unimplemented
	var/maxx = 0
	var/maxy = 0
	var/maxz = 0
	var/icon_size = 32
	var/view = 7 
	var/movement_mode = 0 as opendream_unimplemented
	var/byond_version = 514
	var/byond_build = 1584
	var/version = 0 as opendream_unimplemented
	var/address
	var/port = 0 as opendream_compiletimereadonly
	var/internet_address = "127.0.0.1" as opendream_unimplemented
	var/url as opendream_unimplemented
	var/visibility = 0 as opendream_unimplemented
	var/status as opendream_unimplemented
	var/process
	var/list/params = list() as opendream_unimplemented
	var/sleep_offline = 0 as opendream_unimplemented
	var/system_type
	proc/New()
	proc/Del()
	var/map_cpu = 0 as opendream_unimplemented
	var/hub as opendream_unimplemented
	var/hub_password as opendream_unimplemented
	var/reachable as opendream_unimplemented
	var/game_state as opendream_unimplemented
	var/host as opendream_unimplemented
	var/map_format = 0 as opendream_unimplemented
	var/cache_lifespan = 30 as opendream_unimplemented
	proc/Profile(command, type, format)
		set opendream_unimplemented = 1
	proc/GetConfig(config_set,param)
	proc/SetConfig(config_set,param,value)
	proc/OpenPort(port)
		set opendream_unimplemented = 1
	proc/IsSubscribed(player, type)
		set opendream_unimplemented = 1
	proc/IsBanned(key,address,computer_id,type)
		set opendream_unimplemented = 1
		return 0;
	proc/Error(exception)
		set opendream_unimplemented = 1
	proc/Reboot()
		set opendream_unimplemented = 1
	proc/Repop()
		set opendream_unimplemented = 1
	proc/Export(Addr, File, Persist, Clients)
	proc/Import()
		set opendream_unimplemented = 1
	proc/Topic(T,Addr,Master,Keys)
		set opendream_unimplemented = 1
	proc/SetScores()
		set opendream_unimplemented = 1
	proc/GetScores()
		set opendream_unimplemented = 1
	proc/GetMedal()
		set opendream_unimplemented = 1
	proc/SetMedal()
		set opendream_unimplemented = 1
	proc/ClearMedal()
		set opendream_unimplemented = 1
	proc/AddCredits(player, credits, note)
		set opendream_unimplemented = 1
		return 0
	proc/GetCredits(player)
		set opendream_unimplemented = 1
		return null
	proc/PayCredits(player, credits, note)
		set opendream_unimplemented = 1
		return 0
/atom
	parent_type = /datum
	var/name = "atom"
	var/text = null
	var/desc = null
	var/suffix = null as opendream_unimplemented
	var/list/verbs = list()
	var/list/contents = list()
	var/list/overlays = list()
	var/list/underlays = list()
	var/atom/loc
	var/dir = 2
	var/x = 0
	var/y = 0
	var/z = 0
	var/pixel_x = 0
	var/pixel_y = 0
	var/pixel_z = 0 as opendream_unimplemented
	var/pixel_w = 0 as opendream_unimplemented
	var/icon = null
	var/icon_state = ""
	var/layer = 2.0
	var/plane = -32767 as opendream_unimplemented
	var/alpha = 255
	var/color = "#FFFFFF"
	var/invisibility = 0
	var/mouse_opacity = 1
	var/infra_luminosity = 0 as opendream_unimplemented
	var/luminosity = 0 as opendream_unimplemented
	var/opacity = 0 as opendream_unimplemented
	var/matrix/transform
	var/blend_mode = 0 as opendream_unimplemented
	var/gender = "neuter"
	var/density = 0
	var/maptext as opendream_unimplemented
	var/list/filters = null
	var/appearance as opendream_unimplemented
	var/appearance_flags as opendream_unimplemented
	var/maptext_width as opendream_unimplemented
	var/maptext_height as opendream_unimplemented
	var/maptext_x = 32 as opendream_unimplemented
	var/maptext_y = 32 as opendream_unimplemented
	var/step_x as opendream_unimplemented
	var/step_y as opendream_unimplemented
	var/render_source as opendream_unimplemented
	var/mouse_drag_pointer as opendream_unimplemented
	var/mouse_drop_pointer as opendream_unimplemented
	var/mouse_over_pointer as opendream_unimplemented
	var/render_target as opendream_unimplemented
	var/vis_flags as opendream_unimplemented
	var/list/vis_locs = list() as opendream_unimplemented
	var/list/vis_contents = list() as opendream_unimplemented
	proc/Click(location, control, params)
	proc/DblClick(location, control, params)
		set opendream_unimplemented = 1
	proc/MouseDown(location, control, params)
		set opendream_unimplemented = 1
	proc/MouseDrag(over_object,src_location,over_location,src_control,over_control,params)
		set opendream_unimplemented = 1
	proc/MouseDrop(over_object,src_location,over_location,src_control,over_control,params)
		set opendream_unimplemented = 1
	proc/MouseEntered(location,control,params)
		set opendream_unimplemented = 1
	proc/MouseExited(location,control,params)
		set opendream_unimplemented = 1
	proc/MouseMove(location,control,params)
		set opendream_unimplemented = 1
	proc/MouseUp(location,control,params)
		set opendream_unimplemented = 1
	proc/MouseWheel(delta_x,delta_y,location,control,params)
		set opendream_unimplemented = 1
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
	proc/Stat()
/area
	parent_type = /atom
	layer = 1.0
	luminosity = 1
/mob
	parent_type = /atom/movable
	var/client/client
	var/key
	var/ckey
	var/list/group as opendream_unimplemented
	var/see_invisible = 0
	var/see_infrared = 0 as opendream_unimplemented
	var/sight = 0 as opendream_unimplemented
	var/see_in_dark = 2 as opendream_unimplemented
	layer = 4
	proc/Login()
		client.statobj = src
	proc/Logout()
/atom/movable
	var/screen_loc
	var/animate_movement = 1 as opendream_unimplemented
	var/list/locs = list() as opendream_unimplemented
	var/glide_size as opendream_unimplemented
	var/step_size as opendream_unimplemented
	var/bound_x as opendream_unimplemented
	var/bound_y as opendream_unimplemented
	var/bound_width as opendream_unimplemented
	var/bound_height as opendream_unimplemented
	var/bounds as opendream_unimplemented
	var/particles/particles as opendream_unimplemented
	proc/Bump(atom/Obstacle)
	proc/Move(atom/NewLoc, Dir=0)
		if (isnull(NewLoc) || loc == NewLoc)
			return
		if (Dir != 0)
			dir = Dir
		if (!loc.Exit(src, NewLoc))
			return 0
		for (var/atom/movable/exiting in loc)
			if (!exiting.Exit(src, NewLoc))
				return 0
		if (NewLoc.Enter(src, loc))
			var/atom/oldloc = loc
			var/area/oldarea = oldloc.loc
			var/area/newarea = NewLoc.loc
			loc = NewLoc
			if (newarea != oldarea)
				oldarea.Exited(src, loc)
			oldloc.Exited(src, loc)
			for (var/atom/movable/uncrossed in oldloc)
				uncrossed.Exited(src, loc)
				uncrossed.Uncrossed(src)
			loc.Entered(src, oldloc)
			for (var/atom/movable/crossed in loc)
				crossed.Entered(src, oldloc)
				crossed.Crossed(src)
			if (newarea != oldarea)
				newarea.Entered(src, oldloc)
			return 1
		else
			return 0
/obj
	parent_type = /atom/movable
	layer = 3
/turf
	parent_type = /atom
	layer = 2
	Enter(atom/movable/O, atom/oldloc)
		if (!src.Cross(O))
			return 0
		for (var/atom/content in src.contents)
			if (!content.Cross(O))
				O.Bump(content)
				return 0
		return 1
	Exit(atom/movable/O, atom/newloc)
		return src.Uncross(O)
	Entered(atom/movable/Obj, atom/OldLoc)
		Crossed(Obj)
	Exited(atom/movable/Obj, atom/newloc)
		Uncrossed(Obj)
/proc/bounds(Ref=src, Dist=0)
	set opendream_unimplemented = 1
/proc/bounds_dist(Ref, Target)
	set opendream_unimplemented = 1
/proc/cmptextEx(T1)
	set opendream_unimplemented = 1
/proc/findlasttext_char(Haystack,Needle,Start=0,End=1)
	set opendream_unimplemented = 1
/proc/findlasttextEx_char(Haystack,Needle,Start=1,End=0)
	set opendream_unimplemented = 1
/proc/findtext_char(Haystack,Needle,Start=1,End=0)
	set opendream_unimplemented = 1
/proc/findtextEx_char(Haystack,Needle,Start=1,End=0)
	set opendream_unimplemented = 1
/proc/ftp(File, Name)
	set opendream_unimplemented = 1
/proc/generator(type, A, B, rand)
	set opendream_unimplemented = 1
/proc/issaved(v)
	set opendream_unimplemented = 1
/proc/link(url)
	set opendream_unimplemented = 1
/proc/load_resource(File)
	set opendream_unimplemented = 1
proc/missile(Type, Start, End)
	set opendream_unimplemented = 1
/proc/obounds(Ref=src, Dist=0)
	set opendream_unimplemented = 1
/proc/replacetext_char(Haystack,Needle,Replacement,Start=1,End=0)
	set opendream_unimplemented = 1
/proc/run(File)
	set opendream_unimplemented = 1
/proc/shell(command)
	set opendream_unimplemented = 1
/proc/spantext_char(Haystack,Needles,Start=1)
	set opendream_unimplemented = 1
/proc/nonspantext_char(Haystack,Needles,Start=1)
	set opendream_unimplemented = 1
/proc/splicetext(Text,Start=1,End=0,Insert="")
	set opendream_unimplemented = 1
/proc/splicetext_char(Text,Start=1,End=0,Insert="")
	set opendream_unimplemented = 1
/proc/splittext_char(Text,Start=1,End=0,Insert="")
	set opendream_unimplemented = 1
/proc/text2ascii_char(T,pos=1)
	set opendream_unimplemented = 1
/proc/walk_rand(Ref,Lag=0,Speed=0)
	set opendream_unimplemented = 1
/proc/winclone(player, window_name, clone_name)
	set opendream_unimplemented = 1
/proc/winget(player, control_id, params)
	set opendream_unimplemented = 1
/proc/winshow(player, window, show=1)
	set opendream_unimplemented = 1
/database
	parent_type = /datum
	proc/Close()
	proc/Error()
	proc/ErrorMsg()
	New(filename)
	proc/Open(filename)
/database/query
	var/_binobj as opendream_unimplemented
	proc/Add(text, ...)
	proc/Clear()
	Close()
	proc/Columns(column)
	Error()
	ErrorMsg()
	proc/Execute(database)
	proc/GetColumn(column)
	proc/GetRowData()
	New(text, ...)
	proc/NextRow()
	proc/Reset()
	proc/RowsAffected()
/proc/_dm_db_new_con()
	set opendream_unimplemented = 1
/proc/_dm_db_connect()
	set opendream_unimplemented = 1
/proc/_dm_db_close()
	set opendream_unimplemented = 1
/proc/_dm_db_is_connected()
	set opendream_unimplemented = 1
/proc/_dm_db_quote()
	set opendream_unimplemented = 1
/proc/_dm_db_new_query()
	set opendream_unimplemented = 1
/proc/_dm_db_execute()
	set opendream_unimplemented = 1
/proc/_dm_db_next_row()
	set opendream_unimplemented = 1
/proc/_dm_db_rows_affected()
	set opendream_unimplemented = 1
/proc/_dm_db_row_count()
	set opendream_unimplemented = 1
/proc/_dm_db_error_msg()
	set opendream_unimplemented = 1
/proc/_dm_db_columns()
	set opendream_unimplemented = 1
proc/replacetextEx_char(Haystack, Needle, Replacement, Start = 1, End = 0)
	set opendream_unimplemented = 1
	return Haystack
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
	if (Dir & 1) y += 1
	else if (Dir & 2) y -= 1
	if (Dir & 4) x += 1
	else if (Dir & 8) x -= 1
	if (Dir & 16) z += 1
	else if (Dir & 32) z -= 1
	return locate(x, y, z)
proc/get_dir(atom/Loc1, atom/Loc2)
	if (Loc1 == null || Loc2 == null || Loc1.z != Loc2.z) return 0
	var/dir = 0
	if (Loc2.x < Loc1.x) dir |= 8
	else if (Loc2.x > Loc1.x) dir |= 4
	if (Loc2.y < Loc1.y) dir |= 2
	else if (Loc2.y > Loc1.y) dir |= 1
	return dir
/proc/step(atom/movable/Ref, var/Dir, var/Speed=0)
	Ref.Move(get_step(Ref, Dir), Dir)
/proc/step_away(atom/movable/Ref, /atom/Trg, Max=5, Speed=0)
    Ref.Move(get_step_away(Ref, Trg, Max), turn(get_dir(Ref, Trg), 180))
/proc/step_to(atom/movable/Ref, atom/Trg, Min = 0, Speed = 0)
	var/dist = get_dist(Ref, Trg)
	if (dist <= Min) return
	var/step_dir = get_dir(Ref, Trg)
	step(Ref, step_dir, Speed)
/proc/walk_towards(Ref,Trg,Lag=0,Speed=0)
	set opendream_unimplemented = 1
	CRASH("/walk_towards() is not implemented")
/proc/get_step_to(Ref, Trg, Min=0)
	set opendream_unimplemented = 1
	CRASH("/get_step_to() is not implemented")
/proc/walk_away(Ref,Trg,Max=5,Lag=0,Speed=0)
	set opendream_unimplemented = 1
	CRASH("/walk_away() is not implemented")
/proc/turn(Dir, Angle)
	if (istype(Dir, /matrix))
		var/matrix/copy = new(Dir)
		return copy.Turn(Angle)
	var/dirAngle = 0
	switch (Dir)
		if (4) dirAngle = 0
		if (5) dirAngle = 45
		if (1) dirAngle = 90
		if (9) dirAngle = 135
		if (8) dirAngle = 180
		if (10) dirAngle = 225
		if (2) dirAngle = 270
		if (6) dirAngle = 315
		else
			if (Angle != 0)
				return pick(1, 2, 4, 8, 5, 6, 10, 9)
			else if (!isnum(Dir))
				CRASH("Invalid Dir \"[json_encode(Dir)]\"")
			else
				return Dir
	dirAngle += trunc(Angle/45) * 45
	dirAngle = dirAngle % 360
	if(dirAngle < 0)
		dirAngle = 360 + dirAngle
	switch (dirAngle)
		if (45) return 5
		if (90) return 1
		if (135) return 9
		if (180) return 8
		if (225) return 10
		if (270) return 2
		if (315) return 6
		else return 4
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
	var/dir = pick(1, 2, 4, 8, 5, 6, 10, 9)
	return get_step(Ref, dir)
proc/hearers(Depth = world.view, Center = usr)
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
/datum
	var/idx = 0
	var/c = 0
	proc/do_loop()
		for (src.idx in 1 to 5)
			c += idx
/proc/RunTest()
	var/datum/d = new
	d.do_loop()
	world.log << "[d.c]"
	((d.c == 15) ? null : CRASH("Assertion Failed: " + @#d.c == 15#))
