// Building generators out of a vector that's held in a var must leave that var alone
/proc/RunTest()
	var/vector/v = vector(1, 2)

	var/generator/first = generator("vector", v, v)
	var/generator/second = generator("box", v, v)
	var/generator/third = generator("square", v, v)

	var/vector/result = first.Rand()
	ASSERT(result.x == 1)
	ASSERT(result.y == 2)
	ASSERT(second.Rand() != null)
	ASSERT(third.Rand() != null)

	// The vector is unharmed and still usable
	ASSERT(v.len == 2)
	ASSERT(v.x == 1)
	ASSERT(v.y == 2)

	var/vector/sum = v + vector(1, 1)
	ASSERT(sum.x == 2)
	ASSERT(sum.y == 3)
