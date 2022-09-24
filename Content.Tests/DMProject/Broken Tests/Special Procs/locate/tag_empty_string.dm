
//# issue 647

/obj
	var/const/c = 5

/proc/RunTest()
	var/obj/o = new
	o.tag = "tag"
	ASSERT(locate("tag").c == 5)
	o.tag = ""
	ASSERT(locate("tag") == null)
