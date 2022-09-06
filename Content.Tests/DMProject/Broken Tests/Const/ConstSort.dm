/obj/ConstSortObj
	var/const/ConstSortObj_c3 = static_ConstSortObj.ConstSortObj_c1 + static_ConstSortObj.ConstSortObj_c2

/obj/ConstSortObj
	var/const/ConstSortObj_c2 = static_ConstSortObj.ConstSortObj_b

/obj/ConstSortObj
	var/const/ConstSortObj_c1 = static_ConstSortObj.ConstSortObj_a

/obj/ConstSortObj
	var/static/obj/static_ConstSortObj = new
	var/const/ConstSortObj_a = 7
	var/const/ConstSortObj_b = 8

/proc/RunTest()
	var/obj/ConstSortObj/o = new
	ASSERT(o.ConstSortObj_c1 == 7)
	ASSERT(o.ConstSortObj_c2 == 8)
	ASSERT(o.ConstSortObj_c3 == 15)
	ASSERT(o.ConstSortObj_a == 7)
	ASSERT(o.ConstSortObj_b == 8)
