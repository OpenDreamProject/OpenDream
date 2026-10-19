// A constant RHS only decides x && y when the LHS is a constant too
/proc/RunTest()
	var/zero = 0
	ASSERT(!(zero && 1))
	ASSERT(isnull(null && 1))
	ASSERT((1 && 2) == 2)
