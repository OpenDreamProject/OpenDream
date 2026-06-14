
/datum
	var/const/test = 5

/proc/RunTest()
	var/datum/o = new
	var/a = 0
	switch(a)
		if(o.test)
			ASSERT(FALSE)
		else
			return
	ASSERT(FALSE)
