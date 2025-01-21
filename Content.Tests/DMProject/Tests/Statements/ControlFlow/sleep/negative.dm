
//# issue 532

/proc/RunTest()
	var/st = world.realtime
	var/l = 0
	spawn {
		var/ct = 3
		while(ct > 0) {
			l += 1
			sleep(3)
			ct -= 1
		}
	}
	sleep(-1)
	l += 2
	ASSERT(l == 2)