
/proc/RunTest()
	var/a = 1#INF
	var/b = -1#INF
	var/c = -1#IND
	ASSERT("[a]" == "inf")
	ASSERT("[b]" == "-inf")
	ASSERT("[c]" == "nan")
