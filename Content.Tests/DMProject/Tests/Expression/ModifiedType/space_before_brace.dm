
//# issue 655

/datum
  var/a = 5

/proc/RunTest()
  var/datum/D = new /datum {a=7}
  ASSERT(D.a == 7)