
var/const/blu = rgb(0,0,255)

/proc/RunTest()
	var/const/lblu = rgb(0,0,255)
	ASSERT(blu == "#0000ff")
	ASSERT(lblu == "#0000ff")
