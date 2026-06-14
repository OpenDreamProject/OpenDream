/proc/RunTest()
	var/datum/thing = new()
	var/datum/thing2 = new()
	var/datum/thing3 = new()
	var/not_this_one = "\ref[thing]"
	var/ref = "\ref[thing2]"
	var/not_this_one_either = "\ref[thing]"
	var/test_thing = locate(ref)
	ASSERT(test_thing == thing2)

	var/string = "farts"
	var/string_ref = "\ref[string]"
	ASSERT(locate(string_ref) == string)

	var/proc_ref = "\ref[/proc/RunTest]"
	ASSERT(length(proc_ref)==12)

	var/list/test = list(1,2,3)
	ASSERT(copytext("\ref[test]",4,5) == "f")
	ASSERT(locate("\ref[test]") == test)