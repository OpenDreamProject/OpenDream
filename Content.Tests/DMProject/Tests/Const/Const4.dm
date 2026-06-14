/datum/ConstList1
	var/ConstList1_list = list("a" = 5, "b" = 6)

/proc/RunTest()
	var/datum/ConstList1/o = new
	return o.ConstList1_list["a"]
