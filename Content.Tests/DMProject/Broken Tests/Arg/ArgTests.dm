// Name - Expected Outcome

// ArgEquals1 - No Error
// TODO Failed test. DM doesn't compile
/*/world/proc/ArgEquals1_eqarg(val1)
	ASSERT(ispath(val1, /obj))
/world/proc/ArgEquals1_proc()
	world.ArgEquals1_eqarg(/obj = 2)*/

// ArgEquals2 - No Error
// TODO Failed test. DM doesn't compile
/*/world/proc/ArgEquals2_eqarg(val1, val2)
	ASSERT(ispath(val1, /obj))
	ASSERT(isnull(val2))
/world/proc/ArgEquals2_proc()
	world.ArgEquals2_eqarg(/obj = 2)*/

// ArgEquals3 - No Error
// TODO Failed test. DM doesn't compile
/*/world/proc/ArgEquals3_eqarg(val1, val2, val3)
	ASSERT(ispath(val1, /obj))
	ASSERT(val2 == 6)
	ASSERT(isnull(val3))
/world/proc/ArgEquals3_proc()
	world.ArgEquals3_eqarg(/obj = 2, 6)*/

// ArgProcsProcs1 - No Error
// TODO Failed test. DM doesn't compile
/*/world/proc/ArgProcsProcs1_procs(procs, procs2, procs3 = 3)
	return procs
/world/proc/ArgProcsProcs1_proc()
	ASSERT(world.ArgProcsProcs1_procs(1,2) == 3)
	ASSERT(world.ArgProcsProcs1_procs(1,2,4) == 4)
	ASSERT(world.ArgProcsProcs1_procs("procs"= 4 ) == 3)
	ASSERT(world.ArgProcsProcs1_procs("procs"= 4, "procs"= 5, "procs" = 6) == 3)
	ASSERT(world.ArgProcsProcs1_procs(1, 2, 4, "procs" = 5) == 4)*/
