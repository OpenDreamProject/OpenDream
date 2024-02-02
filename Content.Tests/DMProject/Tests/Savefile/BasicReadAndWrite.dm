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
	S2?["ABC"] << CRASH("rhs should not evaluate due to null-conditional")

	S.cd = "DEF"
	var/out
	ASSERT(S.eof == 0)
	S >> out
	ASSERT(out == 10)
	ASSERT(S.eof == 1)
	S.eof = -1
	S.cd = "/"
	ASSERT(S["DEF"] == null)

	fdel("savefile.sav")
