/holder
  var/const/constant = "correct"

var/global/holder/global_var
var/global/constant = "incorrect"

/_
  var/type_index = /holder::constant
  var/global_index = global_var::constant

/proc/RunTest()
	ASSERT(/_::type_index == "correct")
	ASSERT(/_::global_index == "correct")
