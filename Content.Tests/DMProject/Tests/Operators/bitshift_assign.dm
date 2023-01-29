
// Issue OD#922: https://github.com/OpenDreamProject/OpenDream/issues/922

/proc/RunTest()
	//Easy references
	var/x = 1
	var/y = 2
	x <<= 5
	y <<= 5
	x >>= 4
	y >>= 5
	ASSERT(x == y)
	//Compound references
	var/list/a = list(1,2,3,4,5)
	for(var/i = 1; i < 6; i += 1)
		a[i] <<= 1
		ASSERT(a[i] == (i << 1))
		a[i] >>= 1
		ASSERT(a[i] == i)
	//Make sure cursed behaviour still works in the assignment situation
	var/n = null
	n <<= 1
	ASSERT(n == 0)
	n = null
	n >>= 1
	ASSERT(n == 0)
