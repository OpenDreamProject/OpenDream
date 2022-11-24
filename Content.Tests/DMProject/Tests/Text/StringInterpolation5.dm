

/proc/RunTest()
	var/atom/O = new()
	O.name = "foo"
	O.gender = FEMALE
	var/atom/O2 = new
	O2.name = "foob"
	O2.gender = MALE
	var/text = "[O2], \ref[O], \his"
	ASSERT(findtextEx(text,", his") != 0)
