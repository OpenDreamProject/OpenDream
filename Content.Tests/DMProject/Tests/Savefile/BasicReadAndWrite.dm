/datum/foobar

/proc/RunTest()
	var/savefile/S = new("savefile.sav")
	var/savefile/S2 = null
	var/V

	// Indexing the object to write/read the savefile
	S["ABC"] = 5
	ASSERT(V == null)
	ASSERT(S["ABC"] == 5)
	V = S["ABC"]
	ASSERT(V == 5)

	// << and >> can do the same thing
	S["DEF"] << 10
	S["DEF"] >> V
	ASSERT(S["DEF"] == 10)
	ASSERT(V == 10)
	S["notakey"] >> V
	ASSERT(V == null)
	
	// test path
	S["pathymcpathface"] << /datum/foobar
	ASSERT(S["pathymcpathface"] == /datum/foobar)
	
	// test list()
	var/list/array = list("3.14159", "pizza")
	S["pie"] << array
	ASSERT(S["pie"] ~= array)
	var/list/assoc = list("6.28" = "pizza", "aaaaa" = "bbbbbbb")
	S["pie2"] << assoc
	ASSERT(S["pie2"] ~= assoc)

	// Shouldn't evaluate CRASH
	//NB: using = rather than << here because << *does* evaluate the RHS regardless of the nullness of the LHS
	S2?["ABC"] = CRASH("rhs should not evaluate due to null-conditional")

	// Test EOF
	S.cd = "DEF"
	var/out
	ASSERT(S.eof == 0)
	S >> out
	ASSERT(out == 10)
	ASSERT(S.eof == 1)
	S.eof = -1
	S.cd = "/"
	ASSERT(S["DEF"] == null)

	//Test dir
	S.cd = "/"
	var/dir = S.dir
	ASSERT(dir ~= list("ABC", "DEF", "notakey", "pathymcpathface", "pie", "pie2"))

	//test add
	dir += "test/beep"
	ASSERT(dir ~= list("ABC", "DEF", "notakey", "pathymcpathface", "pie", "pie2", "test"))
	ASSERT(S["test"] == null)
	S.cd = "test"
	ASSERT(dir ~= list("beep"))

	//test del
	S.cd = "/"
	dir -= "test"
	ASSERT(dir ~= list("ABC", "DEF", "notakey", "pathymcpathface", "pie", "pie2"))

	//test rename and null
	dir[1] = "CBA"
	ASSERT(dir ~= list("CBA", "DEF", "notakey", "pathymcpathface", "pie", "pie2"))
	ASSERT(S["CBA"] == null)

	fdel("savefile.sav")

	file("badsavefile.sav") << "hey wait, this isn't json! oh well, better fail miserably and die"
	
	var/savefile/fail
	try
		fail = new("badsavefile.sav")
	catch(var/exception/e)
		ASSERT(isnull(fail))
	ASSERT(fail == null)
	fdel("badsavefile.sav")
