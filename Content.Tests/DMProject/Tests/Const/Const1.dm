/world/proc/Const1_Proc()
	var/const/ConstSwitch_c = 6

	switch (1)
		if (ConstSwitch_c)
			return 0
		else
			return 1

	return 2
