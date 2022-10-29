
//# issue 659

/obj
	var/table[31][51]

/proc/RunTest()
	var/obj/o = new
	o.table[5][6] = 2
	ASSERT(o.table[5][6] == 2)
