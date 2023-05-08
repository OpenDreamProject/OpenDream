/proc/foobar()

/verb/foobarverb()

/datum/proc/foo()

/datum/verb/fooverb()

/datum/proc/bar()

/datum/subtype/bar()

/proc/RunTest()
	ASSERT(text2path("/datum/proc/foo") == /datum/proc/foo)
	ASSERT(text2path("/proc/foobar") == /proc/foobar)
	ASSERT(text2path("/verb/foobar") == null)
	ASSERT(text2path("/verb/foobarverb") == /verb/foobarverb)
	ASSERT(text2path("/datum/subtype/proc/foo") == null)
	ASSERT(text2path("/datum/subtype/bar") == null)
	ASSERT(text2path("/datum/subtype/proc/bar") == null)
	ASSERT(text2path("") == null)
	ASSERT(text2path("    ") == null)
	ASSERT(text2path("complete nonsense") == null)
	ASSERT(text2path("/") == null)
	ASSERT(text2path("/proc/invalid") == null)
	ASSERT(text2path("/datum/proc/fooverb") == null)
	ASSERT(text2path("/datum/verb/fooverb") == /datum/verb/fooverb)
	ASSERT("[text2path("/datum/verb/fooverb")]" == "/datum/verb/fooverb")