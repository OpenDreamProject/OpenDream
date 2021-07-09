/proc/sync_test()
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
	var/L = list(1, 2, 3)
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