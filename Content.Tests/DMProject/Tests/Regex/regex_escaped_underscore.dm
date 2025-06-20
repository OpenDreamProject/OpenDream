/proc/RunTest()
	var/regex/R = regex("\\_", "g")
	CRASH(R.Replace_char("t_e_s_t","0"))
	ASSERT(R.Replace_char("t_e_s_t","0") == "t0e0s0t")