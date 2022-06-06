// ListNullObj2 - No Error
// TODO Failed test - DM compiler error. Fixed in PR #659
/*
/obj/ListNullObj2
	var/a[]
	var/b[5][3]
/world/proc/ListNullObj2_proc()
	var/obj/ListNullObj2/o = new
	ASSERT(!islist(o.a))
	ASSERT(islist(o.b))
	ASSERT(islist(o.b[1]))
	ASSERT(o.b[1].len == 3)*/

// ListNullProc2 - No Error
// TODO Failed test - DM compiler error. Fixed in PR #659
/*
/world/proc/ListNullProc2_proc()
	var/a[]
	var/b[5][3]

	ASSERT(!islist(a))
	ASSERT(islist(b))
	ASSERT(islist(b[1]))
	ASSERT(b[1].len == 3)*/

// ListNullArg2 - Runtime Error
// TODO Failed test - DM compiler error. Fixed in PR #659
/*/world/proc/ListNullArg2(a[5][3])
	ASSERT(a[1].len == 3)
/world/proc/ListNullArg2_Proc()
	world.ListNullArg2()*/
