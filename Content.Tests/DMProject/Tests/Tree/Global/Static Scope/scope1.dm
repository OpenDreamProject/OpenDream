
var/gvar = 3

/obj
	var/static/osvar = gvar

/proc/sproc()
	var/static/psvar = gvar
	ASSERT(psvar == 3)

/proc/RunTest()
	var/obj/o = new
	sproc()
	ASSERT(o.osvar == 3)