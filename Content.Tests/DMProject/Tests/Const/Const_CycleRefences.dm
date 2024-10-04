// COMPILE ERROR OD0404

var/const/A = B
var/const/B = C
var/const/C = A

/proc/RunTest()
    return