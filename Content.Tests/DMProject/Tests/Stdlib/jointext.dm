
//# issue 1086

/proc/RunTest()
	ASSERT(jointext("Hello", ",") == "Hello")
	ASSERT(jointext(list("Hello", "world", "!"), " ") == "Hello world !")
	ASSERT(jointext(list(), "a") == "")
	ASSERT(jointext(list(1, "a"), " ") == "1 a")
	ASSERT(jointext(list(list(),list()), " ") == "/list /list")
	ASSERT(jointext(list(null, ""), " ") == " ")

	var/datum/d = new()
	ASSERT(jointext(list(d), "") == "[d]")
