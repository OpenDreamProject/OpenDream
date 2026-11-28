/proc/RunTest()
	var/obj/O = new
	var/list/L = O.vis_contents
	// vis_contents holds a reference to the owning datum, and is deleted along with it.
	ASSERT(refcount(O) == 2)
	var/list/L2 = O.vis_contents
	ASSERT(refcount(O) == 3) // Just in case, due to how internal lists are exposed.
	del(O)

	ASSERT(isnull(L))
	ASSERT(isnull(O))