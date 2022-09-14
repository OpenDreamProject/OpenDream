
/obj
	var/const/test = 5

/proc/RunTest()
	var/obj/o = new
	var/a = 0
	switch(a)
		if(o.test)
			ASSERT(FALSE)
		else
			return
	ASSERT(FALSE)
