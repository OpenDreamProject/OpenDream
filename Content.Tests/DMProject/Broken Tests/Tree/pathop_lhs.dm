
//# issue 617

/atom/movable
	var/paths = list()
	top

	proc/do_assign()
		paths += .top

/proc/RunTest()
	var/atom/movable/t = new
	t.do_assign()
	ASSERT(length(t.paths) == 1)
	ASSERT(t.paths[1] == /atom/movable/top)
