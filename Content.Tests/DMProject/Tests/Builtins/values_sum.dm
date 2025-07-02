
/proc/RunTest()
	ASSERT(values_sum(null) == 0)
	ASSERT(values_sum(list(5)) == 0)
	ASSERT(values_sum(list(a=2)) == 2)
	ASSERT(values_sum(list(a=2,b=0)) == 2)
	ASSERT(values_sum(list(a=2,b=null)) == 2)
	ASSERT(values_sum(list(a=2,b=list(c=5))) == 2)
	ASSERT(values_sum(list(a=2,b=4.4)) == 6.4)