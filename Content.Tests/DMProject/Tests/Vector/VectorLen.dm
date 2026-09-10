// A vector is only 3D when a third component is actually given
/proc/RunTest()
	var/vector/two = vector(3, 4)
	ASSERT(two.len == 2)
	ASSERT(two.z == 0)

	var/vector/three = vector(3, 4, 5)
	ASSERT(three.len == 3)

	// An explicit null still counts as a third component
	var/vector/nullZ = vector(3, 4, null)
	ASSERT(nullZ.len == 3)
	ASSERT(nullZ.z == 0)

	var/vector/direct = new /vector(3, 4)
	ASSERT(direct.len == 2)
