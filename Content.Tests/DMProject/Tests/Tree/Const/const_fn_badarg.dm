// COMPILE ERROR

/proc/somered()
	return 127

var/const/reddish = rgb(somered(),0,255) // error: constant initializer required

/proc/RunTest()
	var/const/reddish = rgb(somered(),0,0) // error: constant initializer required
