
/datum/foo
/datum/foo/bar

/proc/test_null()
	var/datum/D = new
	var/datum/foo/bar/B = astype(D)
	return isnull(B)

/proc/test_null2()
	var/datum/D = new
	var/datum/foo/bar/B = astype(D, /datum/foo/bar)
	return isnull(B)

/proc/test_type()
	var/datum/foo/bar/B = new
	var/datum/D = astype(B)
	var/datum/foo/F = astype(D)
	return F.type

/proc/test_type2()
	var/datum/foo/bar/B = new
	var/datum/D = astype(B, /datum)
	var/datum/foo/F = astype(D, /datum/foo)
	return F.type

/proc/RunTest()
	ASSERT(test_null())
	ASSERT(test_null2())
	ASSERT(test_type() == /datum/foo/bar)
	ASSERT(test_type2() == /datum/foo/bar)
