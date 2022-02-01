/world
	var/list/contents = list()

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
	var/fps = null
	var/tick_usage

	var/maxx = 0
	var/maxy = 0
	var/maxz = 0
	var/icon_size = 32
	var/view = 5

	var/byond_version = DM_VERSION
	var/byond_build = DM_BUILD
	
	var/version = 0 as opendream_unimplemented

	var/address
	var/port
	var/internet_address = "127.0.0.1" as opendream_unimplemented
	var/url
	var/status as opendream_unimplemented
	var/list/params = null as opendream_unimplemented

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
	var/map_format = TOPDOWN_MAP as opendream_unimplemented
	proc/Profile(command, type, format)
		set opendream_unimplemented = TRUE
	proc/GetConfig(config_set,param)
		set opendream_unimplemented = TRUE
	proc/SetConfig(config_set,param,value)
		set opendream_unimplemented = TRUE
	proc/OpenPort(port)
		set opendream_unimplemented = TRUE
	proc/IsSubscribed(player, type)
		set opendream_unimplemented = TRUE

	proc/Reboot()
		set opendream_unimplemented = TRUE

	proc/Repop()
		set opendream_unimplemented = TRUE

	proc/Export(Addr, File, Persist, Clients)

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
