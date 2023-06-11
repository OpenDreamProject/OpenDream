/proc/RunTest()
	ASSERT(ckey(@"!@#$%^&*()-=[];'\,./_+{}:|<>?`~ABC defá" + "\"\n\t") == "@abcdef")