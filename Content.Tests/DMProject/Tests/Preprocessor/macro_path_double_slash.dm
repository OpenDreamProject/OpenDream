//# issue 2649

// A macro that expands to a path can end up pasted directly after a slash, producing an
// empty path element. BYOND ignores those. This can only happen through macro expansion,
// since a literal "//" in source is a comment.

#define LEADING /datum/first
#define MIDDLE /second
#define TRAILING /first

// "//datum/first"
/LEADING
	var/x = 1

// "/datum/first//second"
/datum/first/MIDDLE
	var/y = 2

// The shape goonstation's namespace macros expand to, which relies on "##" pasting a
// leading slash onto a trailing one.
#define IDENTITY(_ARGS...) ##_ARGS
#define NS_PATH(_NAME) /datum/namespace/##_NAME
#define NS_PATH_I(_NAME) /datum/namespace/##_NAME/##IDENTITY
#define CREATE_NS(a, b) NS_PATH_I(a)(var/##NS_PATH_I(a##__##b)(##b = NS_PATH(a##__##b)))
#define ADD_TO_NS(a, b) NS_PATH_I(a##__##b)

/datum/namespace
/datum/namespace/RADIO
/datum/namespace/RADIO__COL

CREATE_NS(RADIO, COL)
ADD_TO_NS(RADIO, COL)(var/const/BRIG = "#FF5000")

/proc/RunTest()
	var/datum/first/F = new
	ASSERT(F.x == 1)

	var/datum/first/second/S = new
	ASSERT(S.x == 1)
	ASSERT(S.y == 2)

	// "/datum//first" in an expression
	ASSERT(/datum/TRAILING == /datum/first)

	var/datum/namespace/RADIO/N = new
	ASSERT(N.COL == /datum/namespace/RADIO__COL)
	ASSERT(/datum/namespace/RADIO__COL::BRIG == "#FF5000")
