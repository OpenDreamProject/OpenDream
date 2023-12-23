
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
	ASSERT(V == 10)

	// Shouldn't evaluate CRASH
	S2?["ABC"] << CRASH("rhs should not evaluate due to null-conditional")

	fdel("savefile.sav")
