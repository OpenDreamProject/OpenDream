
/atom/movable/foo
	var/const/tick_limit_default = 80
	var/static/current_ticklimit = tick_limit_default
	
/proc/RunTest()
	var/atom/movable/foo/F = new
	ASSERT(F.current_ticklimit == 80)
