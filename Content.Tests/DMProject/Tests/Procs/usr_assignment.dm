/proc/change_usr()
	ASSERT(usr == null)
	usr = new/mob
	ASSERT(usr != null)

/proc/RunTest()
	ASSERT(usr == null)
	change_usr()
	ASSERT(usr == null)