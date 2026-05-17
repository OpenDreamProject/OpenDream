
//# issue 659

/datum
	var/table[31][51]

/proc/RunTest()
	var/datum/o = new
	o.table[5][6] = 2
	ASSERT(o.table[5][6] == 2)
