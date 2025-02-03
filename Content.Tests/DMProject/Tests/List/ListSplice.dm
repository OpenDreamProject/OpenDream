
/proc/RunTest()
	var/list/L = list("foo", "bar", "test", "value")
	L.Splice(2, 4, "lorem", "ipsum", "word", "another word")
	ASSERT(L ~= list("foo","lorem","ipsum","word","another word","value"))
