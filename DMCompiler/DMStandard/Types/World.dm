/world
	var/list/contents = null
	var/list/vars

	var/log = null

	var/area = /area as /area
	var/turf = /turf as /turf
	var/mob = /mob as /mob

	var/name = "OpenDream World"
	var/time
	var/timezone = 0
	var/timeofday
	var/realtime
	var/tick_lag = 1
	var/cpu = 0 as opendream_unimplemented
	var/fps = 10
	var/tick_usage
	var/loop_checks = 0 as opendream_unimplemented

	var/maxx = null as num|null
	var/maxy = null as num|null
	var/maxz = null as num|null
	var/icon_size = 32 as num
	var/view = 5 as text|num
	var/movement_mode = LEGACY_MOVEMENT_MODE as opendream_unimplemented

	var/byond_version = DM_VERSION
	var/byond_build = DM_BUILD

	var/version = 0 as opendream_unimplemented

	var/address
	var/port = 0 as opendream_compiletimereadonly
	var/internet_address = "127.0.0.1" as opendream_unimplemented
	var/url as opendream_unimplemented
	var/visibility = 0 as opendream_unimplemented
	var/status as opendream_unimplemented
	var/process
	var/list/params = null

	var/sleep_offline = 0 as opendream_unimplemented

	var/system_type

	var/map_cpu = 0 as opendream_unimplemented
	var/hub as opendream_unimplemented
	var/hub_password as opendream_unimplemented
	var/reachable as opendream_unimplemented
	var/game_state as opendream_unimplemented
	var/host as opendream_unimplemented
	var/map_format = TOPDOWN_MAP as opendream_unimplemented
	var/cache_lifespan = 30 as opendream_unimplemented
	var/executor as opendream_unimplemented
	
	// An OpenDream read-only var that tells you what port Topic() is listening on
	// Remove OPENDREAM_TOPIC_PORT_EXISTS if this is ever removed
	var/const/opendream_topic_port
	
	proc/New()
	proc/Del()

	proc/Profile(command, type, format)
		set opendream_unimplemented = TRUE
	proc/GetConfig(config_set,param)
	proc/SetConfig(config_set,param,value)
	proc/OpenPort(port)
		set opendream_unimplemented = TRUE
	proc/IsSubscribed(player, type)
		set opendream_unimplemented = TRUE
	proc/IsBanned(key,address,computer_id,type)
		set opendream_unimplemented = TRUE
		return FALSE;

	proc/Error(exception)
		set opendream_unimplemented = TRUE

	proc/Reboot()
		set opendream_unimplemented = TRUE

	proc/Repop()
		set opendream_unimplemented = TRUE

	proc/Export(Addr, File, Persist, Clients)
	proc/Import()
		set opendream_unimplemented = TRUE
	proc/Topic(T,Addr,Master,Keys)

	proc/SetScores()
		set opendream_unimplemented = TRUE

	proc/GetScores()
		set opendream_unimplemented = TRUE

	proc/GetMedal()
		set opendream_unimplemented = TRUE

	proc/SetMedal()
		set opendream_unimplemented = TRUE

	proc/ClearMedal()
		set opendream_unimplemented = TRUE

	proc/AddCredits(player, credits, note)
		set opendream_unimplemented = TRUE
		return 0

	proc/GetCredits(player)
		set opendream_unimplemented = TRUE
		return null

	proc/PayCredits(player, credits, note)
		set opendream_unimplemented = TRUE
		return 0
