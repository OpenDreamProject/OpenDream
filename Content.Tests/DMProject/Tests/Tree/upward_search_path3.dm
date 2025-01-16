
//# issue 617

/datum/foo
/datum/bar/var/meep = .foo
/proc/RunTest()
    var/datum/bar/D = new
    ASSERT(D.meep == /datum/foo)
