/proc/RunTest()
	var/regex/R = regex("\\_", "g")
	ASSERT(R.Replace("t_e_s_t","0") == "t0e0s0t")