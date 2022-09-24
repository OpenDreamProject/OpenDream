
/datum
	var/idx = 0
	var/c = 0
	proc/do_loop()
		for (src.idx in 1 to 5)
			c += idx

/proc/RunTest()
	var/datum/d = new
	d.do_loop()
	ASSERT(d.c == 15)
