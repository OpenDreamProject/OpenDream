
/newtype
	var/\6\*\6 = 37
	var/\3\7 = 68
	var/\38 = 39
	var/\ = 77
	var/\ \3 = 33
	var/\t = 73
	var/\improper = "improper"
	var/\justident = "justident" 

/proc/RunTest()
	var/list/expected = list("6*6", "37", "38", " ", " 3", "    ", "improper", "justident")
	var/newtype/o = new
	for(var/i in 1 to expected.len)
		ASSERT(expected[i] == o.vars[i])
