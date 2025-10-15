var/global/list/global_list = list(late_defined_const)

var/const/late_defined_const3 = late_defined_const2
var/const/late_defined_const2 = late_defined_const
var/const/late_defined_const = 1

/datum/TestObj
	var/a = 1 + const_var
	var/const/const_var = 2

/datum/TestObj/SubType
	a = 4 // Catches a regression introduced in #1550

/proc/RunTest()
	var/datum/TestObj/o = new
	ASSERT(o.const_var == 2)
	ASSERT(global_list[1] == 1)
