/image/subclass
	plane = 123
	icon_state = "subclass"

/proc/RunTest()
	var/image/test = new /image/subclass
	ASSERT(test.plane == 123)
	ASSERT(test.icon_state == "subclass")
	var/image/subclass/test2 = new(icon())
	ASSERT(test2.plane == FLOAT_PLANE)
	ASSERT(test2.icon_state == null)
	var/image/subclass/test3 = new(icon_state="test")
	ASSERT(test3.plane == 123)
	ASSERT(test3.icon_state == "test")