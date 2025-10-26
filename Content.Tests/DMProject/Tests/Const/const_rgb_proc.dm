var/const/ConstProc1_a = rgb(0,0,255)

/proc/RunTest()
	var/const/ConstProc1_b = rgb(0,0,255)
	ASSERT(ConstProc1_a == "#0000ff")
	ASSERT(ConstProc1_b == "#0000ff")
