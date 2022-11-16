
/obj
	proc/TheCall()
		return 7

var/obj/i = new

/proc/RunTest()
	ASSERT(global.i.TheCall() == 7)
