
//# issue 647

/datum
	var/const/c = 5

/proc/RunTest()
	var/datum/o = new
	o.tag = "tag"
	ASSERT(locate("tag").c == 5)
	del(o)
	ASSERT(locate("tag") == null)
