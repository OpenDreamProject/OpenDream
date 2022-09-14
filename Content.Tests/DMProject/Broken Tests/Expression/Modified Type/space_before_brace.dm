
//# issue 655

/obj
  var/a = 5

/proc/RunTest()
  var/obj/o = new /obj {a=7}
  ASSERT(o.a == 7)