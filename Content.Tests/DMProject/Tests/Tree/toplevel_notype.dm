
//# issue 213

/usrtype
	var/a = 5

/proc/RunTest()
	var/usrtype/o = new
	ASSERT(o.type == /usrtype)
