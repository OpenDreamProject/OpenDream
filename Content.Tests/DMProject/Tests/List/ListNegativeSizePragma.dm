// RUNTIME ERROR
#pragma ListNegativeSizeException error

/proc/DecrementList()
	var/list/L = list()
	L.len--

/proc/RunTest()
	DecrementList()