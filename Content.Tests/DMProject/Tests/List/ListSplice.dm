
/proc/RunTest()
	var/list/L = list("foo", "bar", "test", "value")
	L.Splice(2, 4, "lorem", "ipsum", "word", "another word")
	ASSERT(L ~= list("foo","lorem","ipsum","word","another word","value"))

	// Again with list() as the arg
	var/list/L2 = list("foo", "bar", "test", "value")
	L2.Splice(2, 4, list("lorem", "ipsum", "word", "another word"))
	ASSERT(L2 ~= list("foo","lorem","ipsum","word","another word","value"))
