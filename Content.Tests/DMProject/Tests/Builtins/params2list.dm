/proc/RunTest()
	ASSERT(params2list("a;b;c") ~= list(a="", b="", c=""))
	ASSERT(params2list("a;a;a") ~= list(a=list("", "", ""))) // Crazy

	ASSERT(params2list("a=1;b=2") ~= list(a="1", b="2"))
	ASSERT(params2list("a=1;a=2") ~= list(a="2"))
