// TODO Move all of these into test files

/turf/blue

/world/proc/assert_test_pass()
	var/assert_test_a = 5 + 2
	ASSERT(assert_test_a == 7)
	return 1

/world/proc/assert_test_fail()
	var/assert_test_a = 5 + 2
	ASSERT(assert_test_a == "a")
	return 1

/world/proc/sync_test()
	return 1992

/world/proc/async_test()
	sleep(1)
	return 1337

/world/proc/error_test()
	. = 1
	src:nonexistent_proc()
	. = 2

/world/proc/image_test()
	return image('a', "Hello")

/world/proc/crash_test()
	. = 1
	CRASH("This should stop the current proc")
	. = 2

/world/proc/stack_overflow_test()
	. = 1
	while(1)
		stack_overflow_test()
	. = 2


//
// waitfor_1_a() should evaluate to 3
//
/world/proc/waitfor_1_a()
	. = 1
	. = waitfor_1_b()

/world/proc/waitfor_1_b()
	set waitfor = FALSE
	. = 2
	. = waitfor_1_c()
	. = 3
	sleep(1)
	. = 4

/world/proc/waitfor_1_c()
	set waitfor = FALSE
	. = 5
	sleep(1)
	. = 6

//
// waitfor_2_a should evaluate to 2
//
/world/proc/waitfor_2_a()
	. = 1
	. = waitfor_2_b()

/world/proc/waitfor_2_b()
	set waitfor = FALSE
	. = 2
	. = waitfor_2_c()
	. = 3
	sleep(1)
	. = 4

/world/proc/waitfor_2_c()
	set waitfor = TRUE
	. = 5
	sleep(1)
	. = 6

//

/world/proc/default_test(datum/arg = new())
	return arg

/world/proc/crazy_inferred_types()
	var/list/L = list()
	L[new()] = new()

/world/proc/value_in_list(test)
	var/list/L = list(1, 2, 3)
	if (4 in L)
		return FALSE
	if (!(3 in L))
		return FALSE
	return TRUE

/world/proc/call_target()
	return 13

/world/proc/call_test()
	return call(src, "call_target")()

//

/datum/parent
	proc/f(a)
		return a

/datum/parent/child
	f(a)
		return ..()

/world/proc/super_call()
	var/datum/parent/child/C = new()
	. = C.f(127)

/datum/recursive
	var/datum/recursive/inner
	var/val = 2

	proc/get_inner()
		. = inner

/world/proc/conditional_access_test()
	var/datum/recursive/R = new()
	R?.inner?.inner = CRASH("this shouldn't be evaluated")
	R.inner = 1
	return R?.inner

/world/proc/conditional_access_test_error()
	var/datum/recursive/R = new()
	return R?.inner.inner

/world/proc/conditional_call_test()
	var/datum/recursive/R = new()
	return R?.inner?.get_inner(CRASH("this shouldn't be evaluated"))

/world/proc/conditional_call_test_error()
	var/datum/recursive/R = new()
	return R?.inner.get_inner()

/world/proc/conditional_mutate()
	var/datum/recursive/R = null
	R?.val *= CRASH("this shouldn't be evaluated")
	R = new()
	return R?.val *= 2

/world/proc/list_index_mutate()
	var/list/L = list(1, 2, 3)
	return L[2] *= 15

/world/proc/switch_const()
	var/a = 137
	switch(a)
		if(20)
			. = 500
		if(136 | 1)
			. = 1
		if(/datum, /mob)
			. = 300

/world/proc/clamp_value()
	var/out1 = clamp(10, 1, 5)
	if (out1 != 5) return 0
	var/out2 = clamp(-10, 1, 5)
	if (out2 != 1) return 0
	var/out3 = clamp(list(-10, 5, 40, -40), 1, 10)
	for(var/item in out3)
		if (item < 1 || item > 10) return 0
	return 1

/world/proc/md5_test()
	return md5("md5_test")

/world/proc/for_loops_test()
	. = list()
	var/counter = 0
	for(var/i in 1 to 3)
		counter++
	. += counter
	counter = 0

	for(var/i = 1 to 3)
		counter++
	. += counter
	counter = 0

	var/j = 1
	for(,j <= 3,j++)
		counter++
	. += counter
	counter = 0

	j = 1
	for(,j++ <= 3)
		counter++
	. += counter

/matrix/proc/Equals(matrix/mat)
	return src.a == mat.a && src.b == mat.b && src.c == mat.c && src.d == mat.d && src.e == mat.e && src.f == mat.f

/world/proc/test_matrix_add()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)
	var/matrix/N = matrix(7, 8, 9, 10, 11, 12)

	M.Add(N)

	if(!M.Equals(matrix(8, 10, 12, 14, 16, 18)))
		CRASH("Unexpected matrix/Add result")

/world/proc/test_matrix_invert()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)

	M.Invert()

	if(!M.Equals(matrix(-1.6666667, 0.6666667, 1, 1.3333334, -0.33333334, -2)))
		CRASH("Unexpected matrix/Invert result [M.a] [M.b] [M.c] [M.d] [M.e] [M.f]")

	M.Invert()

	if(!M.Equals(matrix(1, 2, 3, 4, 5, 6)))
		CRASH("Unexpected matrix/Invert result2 [M.a] [M.b] [M.c] [M.d] [M.e] [M.f]")

/world/proc/test_matrix_multiply()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)
	var/matrix/N = matrix(7, 8, 9, 10, 11, 12)

	M.Multiply(N)

	if(!M.Equals(matrix(39, 54, 78, 54, 75, 108)))
		CRASH("Unexpected matrix/Multiply result")

/world/proc/test_matrix_scale()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)

	M.Scale(2)

	if(!M.Equals(matrix(2, 4, 6, 8, 10, 12)))
		CRASH("Unexpected matrix/Scale result")

/world/proc/test_matrix_subtract()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)
	var/matrix/N = matrix(7, 8, 9, 10, 11, 12)

	M.Subtract(N)

	if(!M.Equals(matrix(-6, -6, -6, -6, -6, -6)))
		CRASH("Unexpected matrix/Subtract result")

/world/proc/test_matrix_translate()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)

	M.Translate(2)

	if(!M.Equals(matrix(1, 2, 5, 4, 5, 8)))
		CRASH("Unexpected matrix/Translate result")

/world/proc/test_matrix_turn()
	var/matrix/M = matrix(1, 2, 3, 4, 5, 6)

	M.Turn(90)

	if(!M.Equals(matrix(4, 5, 6, -1, -2, -3)))
		CRASH("Unexpected matrix/Turn result")

/world/proc/matrix_operations_test()
	test_matrix_add()
	// Invert currently fails because OpenDream doesn't handle floats the way BYOND does.
	// Uncomment when that's changed.
	// test_matrix_invert()
	test_matrix_multiply()
	test_matrix_scale()
	test_matrix_subtract()
	test_matrix_translate()
	test_matrix_turn()

/world/proc/unicode_procs_test()
	ASSERT(length("😀") == 2)
	ASSERT(length_char("😀") == 1)

	// This is the combination of the Man, Woman, Girl and Boy emojis.
	// It's 1 character per emoji, plus 3 zero-width joiner characters between them.
	ASSERT(length("👨‍👩‍👧‍👦") == 11)
	ASSERT(length_char("👨‍👩‍👧‍👦") == 7)
