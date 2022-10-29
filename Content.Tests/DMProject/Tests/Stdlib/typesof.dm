
/datum/a/b

/proc/RunTest()
	var/list/L = typesof(/datum/a)

	ASSERT(L.len == 2)
	ASSERT(L[1] == /datum/a)
	ASSERT(L[2] == /datum/a/b)
