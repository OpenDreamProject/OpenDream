/datum
	var/a
	var/_f

/datum/New(is_root)
	if (is_root)
		a = new /datum(FALSE)
		_f = new /datum(FALSE)
	else
		a = list()
		_f = list()

/datum/proc/f()
	return _f

var/a = new /datum(TRUE)
var/_f = new /datum(TRUE)

/proc/f()
	return _f

/proc/RunTest()
	var/datum/D = new(TRUE)

	ASSERT( (TRUE?D:f():f():f()) == (D:f()) )
	ASSERT( (FALSE?D:f():f():f()) == (f():f()) )

	ASSERT( (TRUE?D:f().f():f()) == (D:f().f()) )
	ASSERT( (FALSE?D:f().f():f()) == (f()) )

	ASSERT( (TRUE?D.f().f():f()) == (D.f().f()) )
	ASSERT( (FALSE?D.f().f():f()) == (f()) )

	ASSERT( (TRUE?D.a:f():f():f()) == (D.a:f()) )
	ASSERT( (FALSE?D.a:f():f()) == (f()) )

	ASSERT( (TRUE?D.a:f():f()) == (D.a:f()) )
	ASSERT( (FALSE?D.a:f():f()) == (f()) )

	ASSERT( (TRUE?(a):f()) == (a) )
	ASSERT( (FALSE?(a):f()) == (f()) )

	ASSERT( (TRUE?f():f()) == (f()) )
	ASSERT( (FALSE?f():f()) == (f()) )

	var/list/L = list(D)
	ASSERT( (TRUE?(TRUE?L[1]:L[2]):()) == (L[1]) )
