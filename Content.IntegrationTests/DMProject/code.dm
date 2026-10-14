//Setup code - put whatever you need for testing here.
/proc/spawn_pool_reuse_probe(count, datum/payload)
	var/datum/captured = payload
	var/list/completed = list(0)
	. = payload
	for(var/i in 1 to count)
		spawn(-1)
			if(src != world || usr != payload || captured != payload || . != payload)
				CRASH("Spawn did not preserve src, usr, arguments, locals or return value")
			. = captured
			completed[1]++
	return completed[1]

/proc/cancellation_depth_probe(payload)
	var/local_payload = payload
	. = payload
	cancellation_depth_probe(local_payload)

/datum/cancellation_operator/proc/operator+(payload)
	var/local_payload = payload
	. = payload
	return src + local_payload

/proc/cancellation_operator_probe(datum/cancellation_operator/operator_owner, payload)
	return operator_owner + payload

/turf
/turf/border
/mob/test

var/global/list/areas_by_type = list()
/area/New()
	areas_by_type[type] = src
	

//The actual tests
//NOTE: Tests placed in the IntegrationTests suite
// should actually require a normal server in order to work.
// Tests that only check generic DM behaviour that is not icon nor map-facing
// should be done within Content.Tests.
// ALSO NOTE: If you're getting strange results, try rebuilding to make sure this DM file is copied correctly.

//Basic sanity check that the map actually loads correctly.
/proc/test_world_init()
	var/a = 0
	var/b = 0
	for(var/turf/t in world)
		if(istype(t,/turf/border))
			b += 1
		else
			a += 1
	if(a + b != 75)
		CRASH("Map probably failed to load; expected 75 tiles in the map, instead found [a + b].")

/datum/unit_test/proc/RunTest()
	throw EXCEPTION("You must override RunTest()")

/world/New()
	// prepare areas
	for(var/area_subtype in typesof(/area) - /area)
		new area_subtype()
	for(var/subtype in typesof(/datum/unit_test))
		if(subtype == /datum/unit_test) //skip the base class
			continue
		var/datum/unit_test/TEST = new subtype()
		TEST.RunTest()
		del(TEST) //and clean up
	world.log << "IntegrationTests successful, /world/New() exiting..."

/datum/object_initialization_owner_probe
    New()
        return

/proc/object_initialization_new_probe(payload, count)
    var/list/arguments = new/list(count)
    arguments[1] = payload
    try
        var/datum/created = new /datum/object_initialization_owner_probe(arglist(arguments))
        del(created)
        return count == 256
    catch(var/error)
        if(error != "Destination is too short. (Parameter 'destination')")
            throw error
        return count == 257

// Ownership probes: actual assignment opcode dispatch, not helper substitutes.
/datum/operator_divide_fallback
    proc/operator/(rhs)
        return rhs

/datum/operator_multiply_fallback
    proc/operator*(rhs)
        return rhs

/datum/operator_defined
    proc/operator+(rhs)
        return rhs
    proc/operator[](index)
        return index
    proc/operator[]=(index, value)
        return value

/datum/operator_divide_exact
    proc/operator/=(rhs)
        return 73
    proc/operator/(rhs)
        return 19

/datum/operator_multiply_exact
    proc/operator*=(rhs)
        return 73
    proc/operator*(rhs)
        return 19

/proc/operator_divide_fallback_probe(payload)
    var/result = new /datum/operator_divide_fallback
    result /= payload
    return result

/proc/operator_multiply_fallback_probe(payload)
    var/result = new /datum/operator_multiply_fallback
    result *= payload
    return result

/proc/operator_add_defined_probe(payload)
    var/result = new /datum/operator_defined
    return result + payload

/proc/operator_divide_exact_probe(payload)
    var/result = new /datum/operator_divide_exact
    result /= payload
    return result

/proc/operator_multiply_exact_probe(payload)
    var/result = new /datum/operator_multiply_exact
    result *= payload
    return result

/proc/operator_index_defined_probe(payload)
    var/result = new /datum/operator_defined
    return result[payload]

/proc/operator_index_assign_defined_probe(payload)
    var/result = new /datum/operator_defined
    result[payload] = payload
    return payload

// Actual compiled call statements reject stdcall before loading a native library.
/proc/call_ext_string_rejection_probe(payload)
	try
		call("ownership_rejection_unused", "symbol@failure")(payload)
	catch(var/error)
		return error
	return "missing resolver rejection"

/proc/call_ext_byond_rejection_probe(payload)
	try
		call("ownership_rejection_unused", "byond:symbol@failure")(payload)
	catch(var/error)
		return error
	return "missing resolver rejection"
