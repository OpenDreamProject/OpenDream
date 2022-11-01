
/proc/RunTest()
	var/savefile/S = new("savefile.sav")
	var/V

	// Indexing the object to write/read the savefile
	S["ABC"] = 5
	ASSERT(V == null)
	V = S["ABC"]
	ASSERT(V == 5)

	// << and >> can do the same thing
	S["DEF"] << 10
	S["DEF"] >> V
	ASSERT(V == 10)

	fdel("savefile.sav")
