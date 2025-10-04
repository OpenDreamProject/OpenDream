
//# issue 351

#define CSC(x) (1/x)

/proc/RunTest()
    var/datum/CSC = new
    ASSERT(CSC(5) == 0.2)
