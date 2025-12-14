/proc/approx(a,b)
	return abs(a-b) < 0.0005

/datum/unit_test/null_transform_is_identity/RunTest()
	var/obj/testObj = new()
	var/matrix/R = matrix()
	R.Turn(180)

	animate(testObj, transform=R, time=1)
	// transform is 180 Rotated 
	ASSERT(approx(testObj.transform.a, -1))
	ASSERT(approx(testObj.transform.b, 0))
	ASSERT(approx(testObj.transform.c, 0))
	ASSERT(approx(testObj.transform.d, 0))
	ASSERT(approx(testObj.transform.e, -1))
	ASSERT(approx(testObj.transform.f, 0))

	animate(testObj, time=1)
	sleep(1)
	// transform not supplied, remains rotated
	ASSERT(approx(testObj.transform.a, -1))
	ASSERT(approx(testObj.transform.b, 0))
	ASSERT(approx(testObj.transform.c, 0))
	ASSERT(approx(testObj.transform.d, 0))
	ASSERT(approx(testObj.transform.e, -1))
	ASSERT(approx(testObj.transform.f, 0))

	animate(testObj, transform=null, time=1)
	sleep(1)
	// transform explicitly null, is back to identity
	ASSERT(testObj.transform.a == 1)
	ASSERT(testObj.transform.b == 0)
	ASSERT(testObj.transform.c == 0)
	ASSERT(testObj.transform.d == 0)
	ASSERT(testObj.transform.e == 1)
	ASSERT(testObj.transform.f == 0)