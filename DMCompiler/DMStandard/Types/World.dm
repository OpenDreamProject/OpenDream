/world
	var/list/contents = null
	var/list/vars

	var/log = null

	var/area = /area as path(/area)
	var/turf = /turf as path(/turf)
	var/mob = /mob as path(/mob)

	var/name = "OpenDream World"
	var/time as num
	var/timezone = 0 as num
	var/timeofday as num
	var/realtime as num
	var/tick_lag = 1 as num
	var/cpu = 0 as opendream_unimplemented|num
	var/fps = 10 as num
	var/tick_usage
	var/loop_checks = 0 as opendream_unimplemented|num

	var/maxx = null as num|null
	var/maxy = null as num|null
	var/maxz = null as num|null
	var/icon_size = 32 as num
	var/view = 5 as text|num
	var/movement_mode = LEGACY_MOVEMENT_MODE as opendream_unimplemented

	var/byond_version = DM_VERSION
	var/byond_build = DM_BUILD

	var/version = 0 as num|opendream_unsupported //only used to notify users on the hub - unsupported due to no hub

	var/address
	var/port = 0 as opendream_compiletimereadonly|num
	var/internet_address = "127.0.0.1" as opendream_unimplemented
	var/url as opendream_unimplemented
	var/visibility = 0 as num|opendream_unimplemented //used to control server appearing on the hub - this will have to use ServerStatusCode.Offline/Online
	var/status = "" as text|opendream_unimplemented //used to display a message appearing under the server on the hub - this will have to use the server desc on the hub
	var/process
	var/list/params = null

	var/sleep_offline = 0 as num

	var/const/system_type as opendream_noconstfold

	var/map_cpu = 0 as opendream_unimplemented
	var/hub = "" as text|opendream_unsupported //used to mark a game as unique on the hub - unsupported due to no hub
	var/hub_password = "" as text|opendream_unsupported //authentication for above - unsupported due to no hub
	var/reachable as opendream_unimplemented
	var/game_state = 0 as num|opendream_unsupported //used to display server joinability on the hub - does not actually affect joining - unsupported due to no hub
	var/host = "" as text|opendream_unsupported //contains the key of the world's host - unsupported as OD server does not run as a user
	var/map_format = TOPDOWN_MAP as opendream_unimplemented
	var/cache_lifespan = 30 as num|opendream_unsupported //used to control cache expiry in RSC - unsupported due to no RSC
	var/executor = "" as text|opendream_unsupported // DMCGI nonsense - there will be no ODCGI
	
	// An OpenDream read-only var that tells you what port Topic() is listening on
	// Remove OPENDREAM_TOPIC_PORT_EXISTS if this is ever removed
	var/const/opendream_topic_port as opendream_noconstfold
	
	proc/New()
	proc/Del()

	proc/Profile(command, type, format)
		set opendream_unimplemented = TRUE
	proc/GetConfig(config_set,param)
	proc/SetConfig(config_set,param,value)
	proc/OpenPort(port)
		set opendream_unimplemented = TRUE
	proc/IsSubscribed(player, type)
		set opendream_unsupported = "OpenDream does not have a premium tier"
	proc/IsBanned(key,address,computer_id,type)
		set opendream_unimplemented = TRUE
		return FALSE;

	proc/Error(exception)

	proc/Reboot()
		set opendream_unimplemented = TRUE

	proc/Repop()
		set opendream_unimplemented = TRUE

	proc/Export(Addr, File, Persist, Clients)
	proc/Import()
		set opendream_unimplemented = TRUE
	proc/Topic(T,Addr,Master,Keys)

	proc/Tick()
		set opendream_unimplemented = TRUE
		
	proc/SetScores()
		set opendream_unsupported = "OpenDream does not support hub scores"

	proc/GetScores()
		set opendream_unsupported = "OpenDream does not support hub scores"

	proc/GetMedal()
		set opendream_unsupported = "OpenDream does not support hub medals"

	proc/SetMedal()
		set opendream_unsupported = "OpenDream does not support hub medals"

	proc/ClearMedal()
		set opendream_unsupported = "OpenDream does not support hub medals"

	proc/AddCredits(player, credits, note)
		set opendream_unsupported = "OpenDream does not support hub credits"
		return 0

	proc/GetCredits(player)
		set opendream_unsupported = "OpenDream does not support hub credits"
		return null

	proc/PayCredits(player, credits, note)
		set opendream_unsupported = "OpenDream does not support hub credits"
		return 0

	proc/ODHotReloadInterface()

	proc/ODHotReloadResource(var/file_name)