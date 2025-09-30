#include "Shared/DerefTests.dm"

/proc/RunTest()
    var/datum/DerefTest1/o2 = new /datum/DerefTest1
    ASSERT(o2.inner_proc().elefn() == 3)
    ASSERT(o2?.inner_proc()?.elefn() == 3)

    o2.innerobj_ty = new /datum/DerefTest1/inner
    ASSERT(o2:innerobj_ty.ele2 == 4)
