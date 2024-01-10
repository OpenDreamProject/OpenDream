
var/global/foobar
/proc/meep()

/datum/test/two
	var/name = "some name"

/datum/test
	var/datum/test/two/two

/datum/test/proc/testarg(atom/movable/A, B)
	ASSERT(two == "two")
	ASSERT(nameof(A) == "A")
	ASSERT(nameof(B) == "B")

/proc/RunTest()
	var/foo = 5
	ASSERT(nameof(foo) == "foo")
	ASSERT(nameof(/proc/meep) == "meep")
	ASSERT(nameof(/datum/test) == "test")
	ASSERT(nameof(global.foobar) == "foobar")
	var/datum/test/T = new
	ASSERT(nameof(T.two.name) == "name")
	T.testarg(new /datum) // Just for fun we won't pass the arg's declared type
