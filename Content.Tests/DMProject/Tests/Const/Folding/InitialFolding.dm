
// Old DM does not support folding initial(), but we do!

/datum/a
	var/const/explicitly_const = 10
	var/not_as_const = "apples"

/datum/a/field_test()
	var/const/x = initial(not_as_const)
	ASSERT(x == "apples")

/proc/RunTest()
	var/datum/a/A = new
	var/const/x = initial(A.explicitly_const)
	ASSERT(x == 10)
	var/const/y = initial(A.not_as_const)
	ASSERT(y == "apples")
	A.field_test()
