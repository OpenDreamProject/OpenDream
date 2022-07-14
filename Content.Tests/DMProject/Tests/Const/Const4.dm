/obj/ConstList1
	var/ConstList1_list = list("a" = 5, "b" = 6)

/proc/RunTest()
	var/obj/ConstList1/o = new
	return o.ConstList1_list["a"]
