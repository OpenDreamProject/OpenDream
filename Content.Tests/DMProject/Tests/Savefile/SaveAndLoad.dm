/datum/foobar

/proc/RunTest()
	var/savefile/S = new("savefile.sav")
	var/savefile/S2 = null


	// Indexing the object to write/read the savefile
	S["ABC"] = 5
	ASSERT(S["ABC"] == 5)

	S["DEF"] = 10
	ASSERT(S["DEF"] == 10)
	
	// test path 
	S["pathymcpathface"] << /datum/foobar
	ASSERT(S["pathymcpathface"] == /datum/foobar)
	
	// test list()
	var/list/array = list("3.14159", "pizza")
	S["pie"] << array
	ASSERT(S["pie"] ~= array)

	// test assoc list()
	var/list/assoc = list("6.28" = "pizza", "aaaaa" = "bbbbbbb")
	S["pie2"] << assoc
	ASSERT(S["pie2"] ~= assoc)

	S.Flush()

	// test loading
	//gotta copy it because otherwise we're accessing the cache
	fcopy("savefile.sav", "savefile2.sav")
	ASSERT(fexists("savefile2.sav"))
	S2 = new("savefile2.sav")
	ASSERT(S2["ABC"] == 5)
	ASSERT(S2["DEF"] == 10)
	ASSERT(S2["pathymcpathface"] == /datum/foobar)
	ASSERT(S2["pie"] ~= array)
	ASSERT(S2["pie2"] ~= assoc)	


	fdel("savefile.sav")