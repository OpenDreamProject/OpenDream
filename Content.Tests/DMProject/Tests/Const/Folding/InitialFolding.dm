
// Old DM does not support folding initial(), but we do!

/datum/a
	var/const/explicitly_const = 10

/datum/a/field_test()
	var/const/x = initial(explicitly_const)
	ASSERT(x == 10)

/proc/RunTest()
	var/datum/a/A = new
	var/const/x = initial(A.explicitly_const)
	ASSERT(x == 10)
	A.field_test()
