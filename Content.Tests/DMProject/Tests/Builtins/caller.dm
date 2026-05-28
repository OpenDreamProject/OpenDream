#define COMPARE(a, b) if(a != b) {CRASH("Assertion failed: expected [b] got [a]")}

/datum/meep/proc/bar()
    COMPARE("[caller.caller.caller.name]", nameof(/proc/RunTest))

/datum/proc/foo()
    var/datum/meep/M = new
    COMPARE("[caller.name]", nameof(/proc/ihateithere))
    M.bar()

/proc/ihateithere()
	var/datum/D = new
	COMPARE("[caller.name]", nameof(/proc/RunTest))
	D.foo()

/proc/RunTest()
    // RunTest()'s caller is null due to how unit tests are invoked
    ihateithere()
