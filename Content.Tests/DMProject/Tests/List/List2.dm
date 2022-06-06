/world/proc/List2_Proc()
	var/a[]
	var/b[5]

	ASSERT(!islist(a))
	ASSERT(islist(b))
	ASSERT(b.len == 5)
