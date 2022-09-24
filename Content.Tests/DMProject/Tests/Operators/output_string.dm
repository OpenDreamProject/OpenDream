
//# issue 216

/obj/ty/New()
	fn()

/obj/ty/proc/fn(mob/user)
	user << "test"

/proc/RunTest()
	var/obj/ty/o = new

