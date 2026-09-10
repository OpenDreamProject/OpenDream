// /generator/proc/Turn() rotates the vectors a generator produces around the XY plane.
// Unlike most Turn() procs it modifies the generator in place and returns it.

/proc/assert_turn(generator/gen, angle, x, y, z)
	ASSERT(gen.Turn(angle) == gen) // Modified in place

	var/vector/result = gen.Rand()
	ASSERT(result.len == 3)
	ASSERT(abs(result.x - x) < 0.0001)
	ASSERT(abs(result.y - y) < 0.0001)
	ASSERT(abs(result.z - z) < 0.0001)

/proc/unit_vector_generator()
	return generator("vector", vector(1, 0), vector(1, 0))

/proc/RunTest()
	assert_turn(unit_vector_generator(), 90, 0, 1, 0)
	assert_turn(unit_vector_generator(), -90, 0, -1, 0)
	assert_turn(unit_vector_generator(), 180, -1, 0, 0)
	assert_turn(unit_vector_generator(), 45, 0.707107, 0.707107, 0)

	// A full rotation comes back around
	assert_turn(unit_vector_generator(), 360, 1, 0, 0)

	// The Z component is left alone
	assert_turn(generator("vector", vector(1, 0, 7), vector(1, 0, 7)), 90, 0, 1, 7)

	// Turning something already combined with an operator works too
	assert_turn(generator("vector", vector(3, 4), vector(3, 4)) * 2, 90, -8, 6, 0)

	// Turning a number generator does nothing
	var/generator/num = generator("num", 5, 5)
	ASSERT(num.Turn(90) == num)
	ASSERT(num.Rand() == 5)
