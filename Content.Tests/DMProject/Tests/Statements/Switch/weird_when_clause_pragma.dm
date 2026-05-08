//COMPILE ERROR OD3201
// NOBYOND
//test to make sure SuspiciousSwitchCase is working

#pragma SuspiciousSwitchCase error

/proc/RunTest()
	var/x = 5
	switch(x)
		if(1)
			return
		if(4)
			return
		else if(x == 3)
			return
		else
			return
