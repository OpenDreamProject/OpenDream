/image/subclass
	plane = 123
	icon_state = "subclass"

/proc/RunTest()
	var/image/test = new /image/subclass
	ASSERT(test.plane == 123)
	ASSERT(test.icon_state == "subclass")