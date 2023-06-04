/obj/listhaver
	var/list/C[][] = list()
	var/D[] = new()

/proc/RunTest()
	var/a[]
	var/b[5]
	var/obj/listhaver/LH = new /obj/listhaver()
	ASSERT(islist(LH.C))
	ASSERT(islist(LH.D))

	ASSERT(!islist(a))
	ASSERT(islist(b))
	ASSERT(b.len == 5)
