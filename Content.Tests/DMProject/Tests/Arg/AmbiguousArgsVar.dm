/proc/argstest(thing, args, beep)
    ASSERT(args == 2)

/proc/RunTest()
    argstest(1, 2, 3)