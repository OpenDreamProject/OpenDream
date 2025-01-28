// COMPILE ERROR OD0404
//# issue 360

/proc/RunTest()
	if(1)
		bad:
	if(1)
		goto bad
