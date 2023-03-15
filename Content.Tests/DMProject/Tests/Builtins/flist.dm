
// TODO: Test that it lists files in a dir correctly. Right now we only test flist() on a nonexistent dir

/proc/RunTest()
	// Assert that a nonexistent dir returns an empty list
	var/list/L = flist("woiguowejsiojioeh/") // If you create this dir, I'll kill you
	ASSERT(islist(L) && L.len == 0)
