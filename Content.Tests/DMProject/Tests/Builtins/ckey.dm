/proc/RunTest()
	ASSERT(ckey(@"!@#$%^&*()-=[];'\,./_+{}:|<>?`~ABC def�" + "\"\n\t") == "@abcdef")