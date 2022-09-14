/obj/a
  var/list/results = list()

  proc/add_result(r) {
    if (r in results) {
      results[r] += 1
    } else {
      results[r] = 1
    }
  }
  proc/unique_results() {
    return length(results)
  }
  proc/total_results() {
    var/t = 0
    for (var/r in results) {
      t += results[r]
    }
    return t
  }
  proc/args_named1(arg1 = 1, arg2 = 2, arg3 = 3) {
    if (arg1 == 0) {
        add_result("arg1")
    }
    if (arg2 == 0) {
        add_result("arg2")
    }
    if (arg3 == 0) {
        add_result("arg3")
    }  
  }
  proc/args_named2() {
    for (var/arg in args)
      add_result(args[arg])
  }

/proc/RunTest()
    var/obj/a/o1 = new
    o1.args_named1("arg[pick(1,2,3)]" = 0)
    ASSERT(o1.unique_results() == 1)
    var/obj/a/o2 = new
    for(var/i = 0, i < 250, i++)
        o2.args_named1("arg[pick(1,2,3)]" = 0)
    ASSERT(o2.unique_results() == 3)
    ASSERT(o2.total_results() == 250)
    ASSERT("arg1" in o2.results)