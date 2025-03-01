
/proc/RunTest()
	ASSERT(values_product(null) == 1)
	ASSERT(values_product(list(5)) == 1)
	ASSERT(values_product(list(a=2)) == 2)
	ASSERT(values_product(list(a=2,b=0)) == 0)
	ASSERT(values_product(list(a=2,b=null)) == 2)
	ASSERT(values_product(list(a=2,b=list(c=5))) == 2)
	ASSERT(values_product(list(a=2,b=4.4)) == 8.8)