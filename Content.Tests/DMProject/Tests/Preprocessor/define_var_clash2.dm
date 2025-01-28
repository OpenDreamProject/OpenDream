// COMPILE ERROR OD0001

#define A 1
#define B 1

// the / at the beginning makes the difference
var/const/A = 5

/proc/nob()
    ASSERT(B == 1)

/proc/RunTest()
    var/const/B = 8
    ASSERT(A == 1)
    nob()
