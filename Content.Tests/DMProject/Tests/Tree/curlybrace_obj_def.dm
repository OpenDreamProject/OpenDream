/datum/var/test_var = "foo"
/datum/var/test_var_2 = "bar"

/datum/descendant{test_var="foo_overriden"; test_var_2="bar_overriden"}

/proc/RunTest()
	var/datum/descendant/D = new
	ASSERT(D.test_var == "foo_overriden")
	ASSERT(D.test_var_2 == "bar_overriden")
