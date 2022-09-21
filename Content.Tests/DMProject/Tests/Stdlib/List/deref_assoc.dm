
//# issue 262

/obj
	var/a = 5

/proc/RunTest()
	var/list/obj/l = list(x=new /obj,y=new /obj,z=new /obj)
	ASSERT(l["x"].a == 5)
