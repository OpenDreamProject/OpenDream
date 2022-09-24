
//# issue 381

/proc/RunTest()
	var/a = 1
	switch (a) {
		if (1) { return; }
		else { ASSERT(FALSE); }
	}
