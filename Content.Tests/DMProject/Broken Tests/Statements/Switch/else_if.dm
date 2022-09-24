
// TODO: OD (rightfully) complains about "else if" in switch() while byond doesn't, revisit when pragmas are a thing

 //# issue 511
 
/proc/switchproc(v)
	switch (v)
		if (5)
			return
		else if (9)
			return

/proc/RunTest()
	var/x = 5
	var/y = 9
	switchproc(x)
	switchproc(y)
