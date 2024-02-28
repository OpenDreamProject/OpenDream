var/global/list/global_list = list(late_defined_const)

var/const/late_defined_const3 = late_defined_const2
var/const/late_defined_const2 = late_defined_const
var/const/late_defined_const = 1

/obj/TestObj
	var/a = 1 + const_var
	var/const/const_var = 2

/obj/TestObj/SubType
	a = 4

/proc/RunTest()
	var/obj/TestObj/o = new
	ASSERT(o.const_var == 2)
	ASSERT(global_list[1] == 1)
