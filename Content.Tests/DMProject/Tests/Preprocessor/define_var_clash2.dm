// COMPILE ERROR

#define A 1
#define B 1

// the / at the beginning makes the difference
var/const/A = 5

/proc/nob()
    world.log << (B)

/proc/RunTest()
    var/const/B = 8
    world.log << (A)
    nob()
