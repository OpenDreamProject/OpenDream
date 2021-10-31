
/proc/addtext(...) 
/proc/bounds_dist(Ref, Target)
/proc/copytext_char(T,Start=1,End=0)
/proc/link(url)
/proc/filter(type, parameter, ...)
/proc/findlasttext_char(Haystack,Needle,Start=0,End=1)
/proc/findtext_char(Haystack,Needle,Start=1,End=0)
/proc/issaved(v)
/proc/shell(command)
/proc/run(File)
/proc/ftp(File, Name)
/proc/replacetext_char(Haystack,Needle,Replacement,Start=1,End=0)
/proc/rgb2num(color, space)
/proc/spantext_char(Haystack,Needles,Start=1)
/proc/winget(player, control_id, params)
/proc/winexists(player, control_id)
/proc/winclone(player, window_name, clone_name)
/proc/winshow(player, window, show=1)
/proc/walk_rand(Ref,Lag=0,Speed=0)

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

/atom/movable
	var/list/locs = list()
	var/glide_size
	var/list/vis_contents = list()

/client
	var/color = 0
	var/control_freak
	var/mouse_pointer_icon
	var/preload_rsc = 1
	var/fps = 0
	var/dir = NORTH
	var/gender = "neuter"
	var/glide_size
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
	var/list/filters = list()
	var/list/vis_contents = list()

/matrix/proc/Interpolate(Matrix2, t)

/mutable_appearance
	var/transform
	var/plane
	var/name

/obj
	var/maptext_height = 0
	var/maptext_width = 0
	var/appearance

/sound
	var/environment
	var/echo
	var/len

/turf
	var/list/vis_contents = list()

/world
	var/map_cpu = 0
	var/hub
	var/hub_password
	var/reachable
	var/game_state
	var/host
	proc/Profile(command, format)
	proc/Profile(command, type, format)
	proc/GetConfig(config_set,param)
	proc/SetConfig(config_set,param,value)
	proc/Export(Addr,File,Persist,Clients)
	proc/OpenPort(port)
	proc/IsSubscribed(player, type)

/database
	parent_type = /datum
	proc/Close()
	proc/Error()
	proc/ErrorMsg()
	New(filename)
	proc/Open(filename)

/database/query
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