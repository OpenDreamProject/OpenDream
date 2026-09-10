// Comparing values of different types with == / != should never error;
// disparate types are simply never equal.
// Regression test for https://github.com/OpenDreamProject/OpenDream/issues/2675

/datum/equality_test

/proc/RunTest()
	var/res = 'Shared/file.txt'
	var/datum/equality_test/obj = new

	// The exact case from the issue: number vs resource
	ASSERT((0 == res) == FALSE)
	ASSERT((0 != res) == TRUE)
	ASSERT((res == 0) == FALSE)
	ASSERT((res != 0) == TRUE)

	// A resource is still equal to itself (compared by path)
	ASSERT((res == res) == TRUE)
	ASSERT((res != res) == FALSE)

	// Other previously-unhandled cross-type comparisons
	ASSERT(("str" == res) == FALSE)
	ASSERT((res == "str") == FALSE)
	ASSERT((obj == res) == FALSE)
	ASSERT((res == obj) == FALSE)
	ASSERT((/datum == res) == FALSE)
	ASSERT((res == /datum) == FALSE)
