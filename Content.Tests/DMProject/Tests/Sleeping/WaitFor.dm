/proc/RunTest()
	ASSERT(waitfor_2_a() == 2)
	ASSERT(waitfor_1_a() == 3)

proc/waitfor_2_a()
	. = 1
	. = waitfor_2_b()

proc/waitfor_2_b()
	set waitfor = FALSE
	. = 2
	. = waitfor_2_c()
	. = 3
	sleep(1)
	. = 4

proc/waitfor_2_c()
	set waitfor = TRUE
	. = 5
	sleep(1)
	. = 6

proc/waitfor_1_a()
	. = 1
	. = waitfor_1_b()

proc/waitfor_1_b()
	set waitfor = FALSE
	. = 2
	. = waitfor_1_c()
	. = 3
	sleep(1)
	. = 4

proc/waitfor_1_c()
	set waitfor = FALSE
	. = 5
	sleep(1)
	. = 6