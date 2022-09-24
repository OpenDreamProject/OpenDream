/obj/ConstInit1obj
	var/const/ConstInit1_a = 5

var/obj/ConstInit1obj/ConstInit1_obj = new
var/const/ConstInit1_a = ConstInit1_obj.ConstInit1_a

/proc/RunTest()
	return ConstInit1_a
