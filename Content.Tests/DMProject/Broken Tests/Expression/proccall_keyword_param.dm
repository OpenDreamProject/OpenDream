
//# issue 655
//# issue 265

// TODO: We error on this when BYOND doesn't. Revisit this test when we can selectively disable/enable errors with pragmas

/obj/proc/nullproc(null, temp)
	ASSERT(null == 1)
	ASSERT(temp == 2)

/proc/RunTest()
	var/obj/o = new
	o.nullproc(1,2)
