
//# issue 576

/proc/RunTest()
	var/thing="Thing"
	if(thing="Thong")
		ASSERT(thing == "Thong")