// Const Sort 1 - No Error
//TODO Failing test - doesn't compile
/*/obj/ConstSortObj
	var/const/ConstSortObj_c3 = static_ConstSortObj.ConstSortObj_c1 + static_ConstSortObj.ConstSortObj_c2
/obj/ConstSortObj
	var/const/ConstSortObj_c2 = static_ConstSortObj.ConstSortObj_b
/obj/ConstSortObj
	var/const/ConstSortObj_c1 = static_ConstSortObj.ConstSortObj_a
/obj/ConstSortObj
	var/static/obj/static_ConstSortObj = new
	var/const/ConstSortObj_a = 7
	var/const/ConstSortObj_b = 8
/world/proc/ConstSortObj1()
	var/obj/ConstSortObj/o = new
	ASSERT(o.ConstSortObj_c1 == 7)
	ASSERT(o.ConstSortObj_c2 == 8)
	ASSERT(o.ConstSortObj_c3 == 15)
	ASSERT(o.ConstSortObj_a == 7)
	ASSERT(o.ConstSortObj_b == 8)
	return 1*/

// Const Proc 1 - No Error
//TODO Failing test - doesn't compile
/*var/const/ConstProc1_a = rgb(0,0,255)
/world/proc/ConstProc1()
	var/const/ConstProc1_b = rgb(0,0,255)
	ASSERT(ConstProc1_a == "#0000ff")
	ASSERT(ConstProc1_b == "#0000ff")
	return 1*/
