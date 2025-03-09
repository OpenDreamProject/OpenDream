
/proc/RunTest()
	var/list/L = list("a","b"=5,"c"="foo")
	var/i = 1

	for(var/keyQ ,valQ in L) // yes the comma can have whitespace on the wrong side
		ASSERT(L[keyQ] == valQ)
		ASSERT(L[i] == keyQ)
		i++

	ASSERT(i == 4)
	i = 1

	var/foo
	var/bar

	for(foo,bar in L)
		ASSERT(L[foo] == bar)
		ASSERT(L[i] == foo)
		i++

	ASSERT(i == 4)
