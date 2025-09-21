
var/static/a = 2
var/static/b = 3

datum
	var/static/hi = a + b

var/datum/o = new

var/static/gvar = 10
var/static/g = o.hi + gvar

/proc/RunTest()
	ASSERT(g == 15)
