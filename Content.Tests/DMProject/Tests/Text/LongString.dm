/proc/RunTest()
	ASSERT({"A
B
C"} == "A\nB\nC")
	
	ASSERT({" " "} == " \" ")