// TODO Move all of these into test files

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

/world/proc/crazy_inferred_types()
	var/list/L = list()
	L[new()] = new()

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