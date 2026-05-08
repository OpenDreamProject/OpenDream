// RUNTIME ERROR
// NOBYOND
#pragma InitialVarOnPrimitiveException error

/proc/initial_test(obj/A)
	return initial(A.name)

/proc/RunTest()
	initial_test("foo")