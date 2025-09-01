
//# issue 216

/obj/ty/New()
	fn()

/obj/ty/proc/fn(mob/user)
	user << "test"

/datum/unit_test/test_output_string_null/RunTest()
	var/obj/ty/o = new

