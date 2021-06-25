/mob
	parent_type = /atom/movable

	var/client/client
	var/key
	var/ckey


	//TODO Actually implement these vars
	var/see_invisible = 0
	var/sight = 0
	var/see_in_dark = 2

	layer = MOB_LAYER

	proc/Login()
		client.statobj = src

	proc/Logout()
