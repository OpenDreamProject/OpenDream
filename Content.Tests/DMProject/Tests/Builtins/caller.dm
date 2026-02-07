/datum/meep/proc/bar()
    ASSERT(caller.caller.caller.name == "ihateithere")

/datum/proc/foo()
    var/datum/meep/M = new
    ASSERT(caller.name == "ihateithere")
    M.bar()

/proc/ihateithere()
	var/datum/D = new
	ASSERT(caller.name == "RunTest")
	D.foo()

/proc/RunTest()
    // RunTest()'s caller is null due to how unit tests are invoked
    ihateithere()
