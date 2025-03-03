
/proc/RunTest()
	ASSERT(values_dot(list(a=2.4,b=1,c=7),list(a=2,b=4,c=null)) == 8.8)
	ASSERT(values_dot(list(),list(a=2,b=4)) == 0)
	ASSERT(values_dot(list("a"),list(a=2,b=4)) == 0)