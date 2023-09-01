// RUNTIME ERROR
/obj/test
  var/const/A = 1

/proc/RunTest()
  var/obj/test/T = new()
  T.vars["A"] = 2 //ideally should runtime
  ASSERT(T.A == 2) //ideally should fail
