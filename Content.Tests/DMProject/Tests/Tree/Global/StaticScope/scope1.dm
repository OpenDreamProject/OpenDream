
var/gvar = 3

/datum
	var/static/osvar = gvar

/proc/sproc()
	var/static/psvar = gvar
	ASSERT(psvar == 3)

/proc/RunTest()
	var/datum/o = new
	sproc()
	ASSERT(o.osvar == 3)