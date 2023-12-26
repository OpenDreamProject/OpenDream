
//Issue OD#996, kinda: https://github.com/OpenDreamProject/OpenDream/issues/996

/proc/RunTest()
	var/x = 5
	switch(x)
		if(1)
			CRASH("Strange branch chosen in switch statement")
		if(4)
			CRASH("Strange branch chosen in switch statement")
		else if(x == 3)
			CRASH("Parser failed to understand 'else if' in switch block")
		else
			return
	CRASH("Parser failed to understand 'else if' in switch block")