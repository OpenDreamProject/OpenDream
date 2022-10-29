
/proc/RunTest()
	var/regex/R = regex(@"\l")
	var/meep = R.Find("0abc123def")
	ASSERT(meep == 2)
