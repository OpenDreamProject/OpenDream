var/count = 0

/datum/dat
	var/t = 1
	var/f = 0

/proc/side_effect()
	count += 1
	return 5

/proc/RunTest()
	var/datum/dat/o_and = new
	var/datum/dat/o_or = new
	var/datum/dat/on_and = null
	var/datum/dat/on_or = null

	var/v1,v2,v3,v4

	v1 = (o_or.t ||= side_effect())
	v2 = (o_or.f ||= side_effect())
	v3 = (o_and.t &&= side_effect())
	v4 = (o_and.f &&= side_effect())

	ASSERT(v1 == 1)
	ASSERT(v2 == 5)
	ASSERT(v3 == 5)
	ASSERT(v4 == 0)

	ASSERT(count == 2)

	o_and = new
	o_or = new
	v1 = v2 = v3 = v4 = null
	v1 = (o_or?.t ||= side_effect())
	v2 = (o_or?.f ||= side_effect())
	v3 = (o_and?.t &&= side_effect())
	v4 = (o_and?.f &&= side_effect())

	ASSERT(v1 == 1)
	ASSERT(v2 == 5)
	ASSERT(v3 == 5)
	ASSERT(v4 == 0)

	ASSERT(count == 4)

	v1 = v2 = v3 = v4 = 5
	v1 = (on_or?.t ||= side_effect())
	v2 = (on_or?.f ||= side_effect())
	v3 = (on_and?.t &&= side_effect())
	v4 = (on_and?.f &&= side_effect())

	ASSERT(v1 == null)
	ASSERT(v2 == null)
	ASSERT(v3 == null)
	ASSERT(v4 == null)

	ASSERT(count == 4)
