// RUNTIME TRUE
/world/proc/ListNullArg1(a[5])
	ASSERT(a.len == 5)
/world/proc/List1_Proc()
	world.ListNullArg1()
