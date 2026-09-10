// Generators can be chained together with math operators, producing a new generator.
// Constant generators (low == high) are used here so the results are deterministic.

/proc/assert_num(generator/gen, expected)
	var/result = gen.Rand()
	ASSERT(isnum(result))
	ASSERT(result == expected)

// A generator built from an operator always produces a 3D vector, even from 2D operands
/proc/assert_vec(generator/gen, x, y, z)
	var/vector/result = gen.Rand()
	ASSERT(istype(result, /vector))
	ASSERT(result.len == 3)
	ASSERT(result.x == x)
	ASSERT(result.y == y)
	ASSERT(result.z == z)

/proc/RunTest()
	var/generator/five = generator("num", 5, 5)
	var/generator/two = generator("num", 2, 2)
	var/generator/vec = generator("vector", vector(3, 4), vector(3, 4))
	var/generator/vec3d = generator("vector", vector(3, 4, 5), vector(3, 4, 5))

	// Operators return a generator, not a value
	ASSERT(istype(five + two, /generator))

	// Number generators with another generator
	assert_num(five + two, 7)
	assert_num(five - two, 3)
	assert_num(five * two, 10)

	// Number generators with a plain number
	assert_num(five + 3, 8)
	assert_num(five - 3, 2)
	assert_num(five * 3, 15)
	assert_num(five / 2, 2.5)
	assert_num(five ** 2, 25)
	assert_num(-five, -5)

	// null counts as 0
	assert_num(five + null, 5)
	assert_num(five - null, 5)
	assert_num(five * null, 0)

	// Operators can be chained
	assert_num((five + 1) * 2, 12)

	// A number generator reduces a vector operand down to its last component
	assert_num(five + vector(1, 2), 7)
	assert_num(five + vector(1, 2, 3), 8)

	// An uncombined generator keeps producing 2D vectors
	var/vector/plain = vec.Rand()
	ASSERT(plain.len == 2)

	// A number is applied to every component of a vector
	assert_vec(vec + 2, 5, 6, 2)
	assert_vec(vec - 2, 1, 2, -2)
	assert_vec(vec * 2, 6, 8, 0)
	assert_vec(vec / 2, 1.5, 2, 0)
	assert_vec(vec ** 2, 9, 16, 0)
	assert_vec(-vec, -3, -4, 0)
	assert_vec(vec + two, 5, 6, 2)
	assert_vec(vec * two, 6, 8, 0)
	assert_vec(vec3d + 2, 5, 6, 7)

	// Vector generators combine component-wise
	assert_vec(vec + vector(1, 2), 4, 6, 0)
	assert_vec(vec - vector(1, 2), 2, 2, 0)
	assert_vec(vec * vector(2, 3), 6, 12, 0)
	assert_vec(vec3d * vector(2, 2, 2), 6, 8, 10)

	// A list of 2 or 3 numbers works in place of a vector
	assert_vec(vec + list(1, 2), 4, 6, 0)
	assert_vec(vec3d * list(2, 2, 2), 6, 8, 10)

	// Multiplying by a matrix transforms the vector
	assert_vec(vec * matrix(2, 0, 10, 0, 3, 20), 16, 32, 0)

	// A color matrix does a 3D transform, with red,green,blue mapping to x,y,z
	assert_vec(vec3d * list(2,0,0,0, 0,3,0,0, 0,0,4,0, 0,0,0,1, 10,20,30,0), 16, 32, 50)

	// The assignment forms rebind the var to the combined generator
	var/generator/combined = generator("num", 5, 5)
	combined += 2
	assert_num(combined, 7)
	combined -= 1
	assert_num(combined, 6)
	combined *= 2
	assert_num(combined, 12)
	combined /= 4
	assert_num(combined, 3)

	// Operands are re-rolled on every Rand() call
	var/generator/random = generator("num", 0, 1) + generator("num", 10, 20)
	for(var/i in 1 to 20)
		var/result = random.Rand()
		ASSERT(result >= 10 && result <= 21)
