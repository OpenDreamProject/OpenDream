/obj/ConstList1
	var/ConstList1_list = list("a" = 5, "b" = 6)
/world/proc/Const4_Proc()
	var/obj/ConstList1/o = new
	return o.ConstList1_list["a"]
