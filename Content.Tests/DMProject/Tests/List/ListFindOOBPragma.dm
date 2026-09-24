// RUNTIME ERROR
// NOBYOND
#pragma ListFindOutOfBoundsException error

/proc/RunTest()
	var/obj/holder = new
	var/obj/held = new(holder)
	holder.contents.Find(held, 1, 99) // BYOND ignores this OOB End param, the pragma makes it a runtime
