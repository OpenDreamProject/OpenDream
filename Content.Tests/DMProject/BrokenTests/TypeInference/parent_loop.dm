// COMPILE ERROR

/datum/a
    // this is specific to parent_type
    parent_type = /datum/b

/datum/b
    parent_type = /datum/a

/proc/RunTest()
    return
