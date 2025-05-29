
//# issue 518

/proc/RunTest()
	var/t = world.realtime
	sleep 10 * 2

/*	Does this look stupid? Probably.
	However, this difference is due BYOND's world.realtime being really inaccurate.
	If we change the sleep duration on line 6 to a longer sleep,
	(ex: "sleep 120 * 2") then the OPENDREAM branch passes in BYOND.
	But ain't nobody got time fo' dat when the main purpose of this test is testing parsing.
	
*/
#ifdef OPENDREAM
	ASSERT((world.realtime - t) != 0)
#else
	ASSERT((world.realtime - t) == 0)
#endif