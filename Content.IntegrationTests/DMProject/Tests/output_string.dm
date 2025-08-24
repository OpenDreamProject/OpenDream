
//# issue 216

/obj/ty/New()
	fn()

/obj/ty/proc/fn(mob/user)
	user << "test"

/proc/test_output_string_null()
	var/obj/ty/o = new

