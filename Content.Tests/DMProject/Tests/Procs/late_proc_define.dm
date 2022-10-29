
// issue OD#869
// https://github.com/OpenDreamProject/OpenDream/issues/869

var/static/earlybird = proc_that_has_value()

/proc/proc_that_has_value()
    return 7

/proc/RunTest()
    ASSERT(earlybird == 7)
