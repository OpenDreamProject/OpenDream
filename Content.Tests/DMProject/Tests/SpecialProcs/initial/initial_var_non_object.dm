/proc/RunTest()
	var/obj/A = "foo" // Lie about the type
	ASSERT(initial(A.name) == null)
