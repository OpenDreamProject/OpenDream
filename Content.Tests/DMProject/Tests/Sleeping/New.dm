var/call_number = 0

/proc/AssertCallNumber(num)
	call_number++
	ASSERT(call_number == num)

/datum/object
	var/datum/inner_object/i = new

	New()
		AssertCallNumber(4)
		sleep(0)
		AssertCallNumber(5)

/datum/inner_object
	New()
		AssertCallNumber(2)
		sleep(0)
		AssertCallNumber(3)

/proc/RunTest()
	AssertCallNumber(1)
	var/datum/object/o = new
	AssertCallNumber(6)
