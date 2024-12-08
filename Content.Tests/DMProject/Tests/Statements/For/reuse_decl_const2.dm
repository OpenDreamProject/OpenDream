// COMPILE ERROR OD0501

/datum
	var/const/idx = 0
	var/c = 0
	proc/do_loop()
		for (idx in list(1,2,3))
			c += idx

/proc/RunTest()
	var/datum/d = new
	d.do_loop()

	var/const/idx = 0
	var/c = 0
	for (idx in list(1,2,3))
		c += idx

