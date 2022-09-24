
//# issue 466

/proc/RunTest()
	var/i = 0
	label_name:
		if(i < 2)
			i += 1
			goto label_name

	ASSERT(i == 2)
