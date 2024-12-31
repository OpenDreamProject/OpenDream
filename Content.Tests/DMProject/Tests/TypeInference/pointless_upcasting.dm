
/datum/later
	var/datum/pointless_base/a

/datum/pointless_base/derived/var/x = 7 // Oh no, this definition is a bit late and quirky!

/proc/RunTest()
	var/datum/later/L = new
	L.a = new /datum/pointless_base/derived()
	ASSERT(L.a:x == 7)
