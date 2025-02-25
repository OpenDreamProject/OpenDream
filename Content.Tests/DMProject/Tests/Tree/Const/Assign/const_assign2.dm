// COMPILE ERROR OD0501

/obj
	/var/const/a = 5

/proc/RunTest()
	var/obj/o = new
	o.a = 6
