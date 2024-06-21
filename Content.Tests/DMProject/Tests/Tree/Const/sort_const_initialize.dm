
/obj
	var/const/c3 = se.c1 + se.c2

/obj
	var/const/c2 = se.b

/obj
	var/const/c1 = se.a

/obj
	var/static/obj/se = new
	var/const/a = 7
	var/const/b = 8

/proc/RunTest()
	var/obj/o = new
	ASSERT(o.c1 == 7)
	ASSERT(o.c2 == 8)
	ASSERT(o.c3 == 15)

	ASSERT(o.a == 7)
	ASSERT(o.b == 8)
