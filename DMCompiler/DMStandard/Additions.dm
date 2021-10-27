
/atom/movable/var/list/vis_contents = list()
/turf/var/list/vis_contents = list()
/image/var/list/vis_contents = list()

/proc/walk_rand(Ref,Lag=0,Speed=0)
/proc/findtext_char(Haystack,Needle,Start=1,End=0)
/proc/link(url)
/proc/filter(type, parameter, ...)
/proc/shell(command)
/proc/run(File)
/proc/ftp(File, Name)
/proc/spantext_char(Haystack,Needles,Start=1)
/proc/winget(player, control_id, params)
/proc/winexists(player, control_id)
/proc/winclone(player, window_name, clone_name)
/proc/winshow(player, window, show=1)
/proc/copytext_char(T,Start=1,End=0)
/proc/bounds_dist(Ref, Target)

/atom
	var/filters = list()
	var/appearance
	var/appearance_flags
	var/maptext_width
	var/maptext_height
	var/maptext_x = 32
	var/maptext_y = 32
	var/pixel_x
	var/pixel_y
	var/pixel_z
	var/pixel_w
	var/step_x
	var/step_y
	var/render_source
	var/bound_width
	var/bound_height
	var/mouse_drag_pointer
	var/mouse_drop_pointer
	var/render_target
	var/vis_flags
	var/vis_locs = list()

/client
	var/color = 0
	var/control_freak
	var/mouse_pointer_icon
	var/preload_rsc = 1
	var/fps = 0
	var/dir = NORTH
	var/gender = "neuter"
	proc/SoundQuery()
	proc/Export(file)
	proc/MeasureText(text, style, width=0)

/image
	var/appearance
	var/maptext_width
	var/maptext_height
	var/maptext_x
	var/maptext_y
	var/bound_width
	var/bound_height
	var/appearance_flags
	var/list/underlays = list()
	var/name
	var/x
	var/y
	var/z
	var/pixel_x
	var/pixel_y
	var/pixel_z
	var/pixel_w
	var/plane = 0
	var/maptext = 0
	var/text = ""
	var/mouse_opacity

/sound/var/environment
/sound/var/echo
/sound/var/len

/matrix/Interpolate(Matrix2, t)

/world/var/map_cpu = 0
/world/var/hub
/world/var/hub_password
/world/var/reachable
/world/var/game_state
/world/var/host
/world/Profile(command, format)
/world/Profile(command, type, format)
/world/GetConfig(config_set,param)
/world/SetConfig(config_set,param,value)
/world/Export(Addr,File,Persist,Clients)
/world/OpenPort(port)
/world/IsSubscribed(player, type)

/obj/var/maptext_height = 0
/obj/var/maptext_width = 0
/obj/var/appearance

/mutable_appearance
	var/transform
	var/plane
	var/name

