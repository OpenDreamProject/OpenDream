
//# issue 1086

// if(1) else the compiler will fucking cry Bloody Mary
// about "invalid expression"
#define SHOULD_CRASH(x) if(1) { \
	var/E = null; \
	var/v = null; \
	try { v = x; } \
	catch(var/exception/e) { E = e }\
	ASSERT(v == null); \
	ASSERT(E); \
}

/proc/RunTest()
	ASSERT(jointext("Hello", ",") == "Hello")
	ASSERT(jointext(list("Hello", "world", "!"), " ") == "Hello world !")
	ASSERT(jointext(list(), "a") == "")
	ASSERT(jointext(list(1, "a"), " ") == "1 a")
	ASSERT(jointext(list(list(),list()), " ") == "/list /list")
	ASSERT(jointext(list(null, ""), " ") == " ")

	var/datum/d = new()
	ASSERT(jointext(list(d), "") == "[d]")
	
	SHOULD_CRASH(jointext(null, ""))
	SHOULD_CRASH(jointext(0, ""))
	SHOULD_CRASH(jointext(TRUE, ""))
	SHOULD_CRASH(jointext(d, ""))
	SHOULD_CRASH(jointext(/datum, ""))
