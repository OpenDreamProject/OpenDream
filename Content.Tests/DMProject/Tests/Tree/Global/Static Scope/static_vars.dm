
var/c = 999

/proc/init_static()
	return dynamic_value()

/proc/dynamic_value()
	static_proc()
	return --c

/proc/static_proc_shadowed()
	var/static/inner_c = 700
	return inner_c

/proc/static_proc()
	// Inner static definitions must be recursively located
	while (1)
		var/static/inner_c = init_static()
		inner_c++
		return inner_c

/obj
	var/const/c = 5
	subobj1
		var/static/s = dynamic_value()
		var/global/g = dynamic_value()
	subobj2
		var/static/s = dynamic_value()
		var/global/g = dynamic_value()

/proc/RunTest()
	var/obj/subobj1/o1 = new
	var/obj/subobj1/o2 = new
	var/obj/subobj2/o3 = new

	ASSERT(static_proc() == 1003)
	ASSERT(static_proc_shadowed() == 700)
	ASSERT(o1.s == 997)
	ASSERT(o1.g == 996)
	ASSERT(o1.c == 5)
	o1.s -= 90
	ASSERT(o2.s == 907)
	ASSERT(o2.g == 996)
	ASSERT(o3.s == 995)
	ASSERT(o3.g == 994) 
