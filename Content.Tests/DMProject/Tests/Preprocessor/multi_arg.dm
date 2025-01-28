// COMPILE ERROR OD0001

//# issue 14

#define TUPLE_GET(x) x(_GETTER)
#define _GETTER(a, ...) a

/proc/RunTest()
    var/test = TUPLE_GET(1,2)
