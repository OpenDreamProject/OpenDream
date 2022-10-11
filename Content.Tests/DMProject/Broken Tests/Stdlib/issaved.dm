
// TODO: This test needs further cleanup/validation but I cba and we need more issaved() tests

//# issue 684

/obj
	var/V
	var/const/C

	proc/log_vars()
		for(var/vname in vars)
			world.log << (issaved(vars[vname]))

/proc/RunTest()
	var/obj/o = new
	o.log_vars()
	ASSERT(FALSE) // To ensure this test fails until it's been revisited (in case we add CI to check broken tests)
