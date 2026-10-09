//# issue 2648

// Only the override form of a proc definition shares a path with a type.
// Everything below compiles in BYOND.

/datum/proc/beep()
	return 1

/datum/test/proc/boop()
	return 2

// A type sharing its name with a proc of the parent type
/datum/test/beep
	var/thing = 1

// A type sharing its name with a proc of its own type
/datum/test/boop
	var/thing = 2

// A type declared after the override that shares its path.
// BYOND doesn't error here, though it does do something strange with the type's vars, so don't test those.
/datum/other/beep()
	return 3

/datum/other/beep
	var/thing = 3

/proc/RunTest()
	var/datum/test/t = new
	ASSERT(t.beep() == 1)
	ASSERT(t.boop() == 2)

	var/datum/other/o = new
	ASSERT(o.beep() == 3)

	var/datum/test/beep/b = new
	ASSERT(b.thing == 1)

	var/datum/test/boop/p = new
	ASSERT(p.thing == 2)
