/proc/RunTest()
	var/obj/thing = new()
	var/obj/thing2 = new()
	var/obj/thing3 = new()
	var/ref = "\ref[thing2]"
	var/test_thing = locate(ref)
	ASSERT(test_thing == thing2)