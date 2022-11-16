// COMPILE ERROR

/proc/somered()
	return 127

/proc/RunTest()
	var/const/reddish = rgb(somered(),0,0) // error: constant initializer required
