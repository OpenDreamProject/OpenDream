/proc/RunTest()
	ASSERT(params2list("a;b;c") ~= list(a="", b="", c=""))
	ASSERT(json_encode(params2list("a;a;a")) == @#{"a":["","",""]}#)

	ASSERT(params2list("a=1;b=2") ~= list(a="1", b="2"))
	ASSERT(params2list("a=1;a=2") ~= list(a="2"))
