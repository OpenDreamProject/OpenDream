
//# issue 610

var/global/G1 = 5
var/global/G2 = G1

/proc/RunTest()
	ASSERT(G2 == 5)
