
//# issue 651

/obj/a
	proc/la(a,b)
		ASSERT(a == 6)
		ASSERT(b == 2)
	b
		la(a,b)
			ASSERT(a == 1)
			ASSERT(b == 2)
			a = 6
			..()

/proc/RunTest()
	var/obj/a/b/o = new
	o.la(1,2)