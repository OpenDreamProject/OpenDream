// Isolated DM fixture, compiled only by TypesOfOwnershipTests; excluded from ordinary NoRuntimes.
/datum/typesof_owner_probe

/datum/typesof_owner_probe/subtype

/proc/typesof_reject_caught(payload, payload_list, path, position)
    try
        switch(position)
            if(1)
                typesof(path, /datum/typesof_owner_probe, payload, payload_list, payload, payload_list)
            if(2)
                typesof(/datum/typesof_owner_probe, path, payload, payload_list, payload, payload_list)
            if(3)
                typesof(/datum/typesof_owner_probe, payload, payload_list, payload, payload_list, path)
    catch(var/exception/error)
        if(error.name == "Object '/__typesof_ownership_missing' does not exist")
            return 17
        return -17
    return 0

/proc/typesof_reject_callee(payload, payload_list, path, position)
    switch(position)
        if(1)
            typesof(path, /datum/typesof_owner_probe, payload, payload_list, payload, payload_list)
        if(2)
            typesof(/datum/typesof_owner_probe, path, payload, payload_list, payload, payload_list)
        if(3)
            typesof(/datum/typesof_owner_probe, payload, payload_list, payload, payload_list, path)
    return 0

/proc/typesof_reject_unwind(payload, payload_list, path, position)
    try
        return typesof_reject_callee(payload, payload_list, path, position)
    catch(var/exception/error)
        if(error.name == "Object '/__typesof_ownership_missing' does not exist")
            return 17
        return -17

/proc/typesof_success_return(payload, payload_list)
    return typesof(/datum/typesof_owner_probe, payload, payload_list, payload, payload_list)

/proc/typesof_success_drop(payload, payload_list)
    typesof(/datum/typesof_owner_probe, payload, payload_list, payload, payload_list)
    return 17

/proc/typesof_empty_return()
    return typesof()
