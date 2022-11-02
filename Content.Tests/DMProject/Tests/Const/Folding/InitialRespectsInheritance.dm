
// Old DM does not support folding initial(), but we do!

/datum/a
	var/txt = "blumpus"
/datum/a/proc/initialTxt()
	return initial(txt)

/datum/a/b
	txt = "blongo"

/proc/RunTest()
	var/datum/a/b/D = new
	ASSERT(D.initialTxt() == "blongo")
	var/datum/a/SecretlyD = D
	ASSERT(initial(SecretlyD.txt) == "blongo")
