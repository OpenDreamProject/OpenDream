/datum/overloader
	var/list/L = list()

/datum/overloader/proc/operator[](var/idx)
	return L[idx]

/datum/overloader/proc/operator[]=(var/idx, var/d)
	L[idx] = d

/proc/RunTest()
	var/datum/overloader/O = new()
	O["A"] = 5
	O["A"]++

	ASSERT(O["A"] == 6)
