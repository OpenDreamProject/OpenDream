// Name - Expected Outcome


// Const Switch 1 - No Error
/world/proc/ConstSwitch_1()
	var/const/ConstSwitch_c = 6

	switch (1)
		if (ConstSwitch_c)
			return 0
		else
			return 1

	return 2

// Const Div Zero 1 - Runtime Error
var/ConstZero1_a
/world/proc/ConstZero1()
	ConstZero1_a = 1 / ConstZero1()
	return 1

// Const Div Zero 2 - Runtime Error
var/ConstZero2_a
var/ConstZero2_b = 0
/world/proc/ConstZero2()
	ConstZero2_a = 1 / ConstZero2_b
	return 1

// Const List 1 - No Error
/obj/ConstList1
	var/ConstList1_list = list("a" = 5, "b" = 6)
/world/proc/ConstList1()
	var/obj/ConstList1/o = new
	return o.ConstList1_list["a"]

// Const Proc 1 - No Error
//TODO Failing test - doesn't compile
/*var/const/ConstProc1_a = rgb(0,0,255)
/world/proc/ConstProc1()
    var/const/ConstProc1_b = rgb(0,0,255)
    ASSERT(ConstProc1_a == "#0000ff")
    ASSERT(ConstProc1_b == "#0000ff")
    return 1*/

// Const Init 1 - No Error
/obj/ConstInit1obj
	var/const/ConstInit1_a = 5
var/obj/ConstInit1obj/ConstInit1_obj = new
var/const/ConstInit1_a = ConstInit1_obj.ConstInit1_a
/world/proc/ConstInit1()
	return ConstInit1_a

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
