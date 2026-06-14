#include "Shared/DerefTests.dm"

/proc/DerefTest1_new_obj()
    return new /datum/DerefTest1

/proc/RunTest()
    ASSERT(DerefTest1_new_obj().ele == 2)
    ASSERT(DerefTest1_new_obj()?.ele == 2)

    ASSERT(DerefTest1_new_obj().inner_proc().ele == 2)
    ASSERT(DerefTest1_new_obj()?.inner_proc()?.ele == 2)
