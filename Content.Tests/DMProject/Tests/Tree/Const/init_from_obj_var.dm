
/obj
	var/const/a = 5

var/obj/o = new

var/const/a = o.a

/proc/RunTest()
	ASSERT(a == 5)
