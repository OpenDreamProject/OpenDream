/datum/test
	var/datum_val = 1234

/proc/RunTest()
	var/a = 5
	a := 10
	ASSERT(a == 10)

	var/datum/test/b = new()
	var/datum/test/c = new()
	b.datum_val = 4321
	c := b
	ASSERT(c.datum_val == 4321)
