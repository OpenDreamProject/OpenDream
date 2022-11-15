// COMPILE ERROR

/proc/somered()
	return 127

var/const/reddish = rgb(somered(),0,255) // error: constant initializer required

/proc/RunTest()
	return
