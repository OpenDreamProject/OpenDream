
//# issue 262

/datum
	var/a = 5

/proc/RunTest()
	var/list/datum/l = list(x=new /datum,y=new /datum,z=new /datum)
	ASSERT(l["x"].a == 5)
