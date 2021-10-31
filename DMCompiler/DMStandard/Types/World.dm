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
	var/cpu = 0
	var/fps = null
	var/tick_usage

	var/maxx = 0
	var/maxy = 0
	var/maxz = 0
	var/icon_size = 32
	var/view = 5

	var/byond_version = DM_VERSION
	var/byond_build = DM_BUILD

	var/address
	var/port
	var/internet_address = "127.0.0.1" //TODO
	var/url
	var/status
	var/list/params = null

	var/sleep_offline = 0 //TODO

	var/system_type

	proc/New()
	proc/Del()

	var/map_cpu = 0
	var/hub
	var/hub_password
	var/reachable
	var/game_state
	var/host
	proc/Profile(command, type, format)
		set opendream_unimplemented = TRUE
	proc/GetConfig(config_set,param)
		set opendream_unimplemented = TRUE
	proc/SetConfig(config_set,param,value)
		set opendream_unimplemented = TRUE
	proc/Export(Addr,File,Persist,Clients)
		set opendream_unimplemented = TRUE
	proc/OpenPort(port)
		set opendream_unimplemented = TRUE
	proc/IsSubscribed(player, type)
		set opendream_unimplemented = TRUE

	proc/Reboot()
		set opendream_unimplemented = TRUE
		CRASH("/world.Reboot() is not implemented")

	proc/Repop()
		set opendream_unimplemented = TRUE
		CRASH("/world.Repop() will not be implemented")

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


