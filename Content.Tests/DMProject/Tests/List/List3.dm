/obj/ListNullObj1
	var/a[]
	var/b[5]
/world/proc/List3_Proc()
	var/obj/ListNullObj1/o = new
	ASSERT(!islist(o.a))
	ASSERT(islist(o.b))
	ASSERT(o.b.len == 5)
