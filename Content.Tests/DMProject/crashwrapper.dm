//this is so we can use CRASH(X) in list index lookups like L[CRASH("accessed index")]
/proc/_crash_wrapper(X)
    CRASH(X)
    return 0
#define CRASH(X) _crash_wrapper(X)