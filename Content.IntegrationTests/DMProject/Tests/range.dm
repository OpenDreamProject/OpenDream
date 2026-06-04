// tests all of range's possible cases

#define LOC(x, y) locate(x, y, 3)

/obj/contained/one
/obj/contained/two

/datum/unit_test/range/proc/run_case(atom/center, list/expected, identifier, isorange, view_size = 1)
  var/error_index = 0
  var/list/result = isorange ? orange(center, view_size) : range(center, view_size)
  try
    if(result.len != expected.len)
      error_index = expected.len
      CRASH("result is [result.len > expected.len ? "longer" : "shorter"] than expected")
    for(var/index in 1 to result.len)
      if(result[index] != expected[index])
        error_index = index
        CRASH("result does not match expected")
  catch(var/exception/exc)
    var/list/error_output = list()
    error_output += "[identifier]: [exc]"
    error_output += "expected:"
    for(var/i in 1 to expected.len)
      var/atom/A = expected[i]
      error_output += ("\t([A.x], [A.y]) [A.type] [i == error_index ? "<-- here" : null]")
    error_output += "got:"
    for(var/i in 1 to result.len)
      var/atom/A = result[i]
      error_output += ("\t([A.x], [A.y]) [A.type] [i == error_index ? "<-- here" : null]")
    

    CRASH(error_output.Join("\n"))

// Tests the implementation of range() and orange()
/datum/unit_test/range/RunTest()
	world.maxx = world.maxy = 5

	var/turf/center = LOC(3, 3)
	var/area/outer_area = areas_by_type[/area]
	var/obj/container = new /obj(center)
	var/obj/contained = new /obj/contained/one(container)
	var/obj/contained_trash_1 = new /obj/contained/two(contained)
	var/obj/contained_trash_2 = new /obj/contained/two(contained)
	var/obj/contained_trash_3 = new /obj/contained/two(contained)

	var/list/turf_range_case = list(
		LOC(3, 3),
		outer_area,
		container,
		LOC(2, 2),
		LOC(2, 3),
		LOC(2, 4),
		LOC(3, 2),
		LOC(3, 4),
		LOC(4, 2),
		LOC(4, 3),
		LOC(4, 4),
	)

	var/list/turf_orange_case = list(
		LOC(2, 2),
		outer_area,
		LOC(2, 3),
		LOC(2, 4),
		LOC(3, 2),
		LOC(3, 4),
		LOC(4, 2),
		LOC(4, 3),
		LOC(4, 4),
	)

	var/list/container_range_case = list(
		contained,
		LOC(3, 3),
		outer_area,
		container,
		LOC(2, 2),
		LOC(2, 3),
		LOC(2, 4),
		LOC(3, 2),
		LOC(3, 4),
		LOC(4, 2),
		LOC(4, 3),
		LOC(4, 4),
	)

	var/list/container_orange_case = list(
		LOC(3, 3),
		outer_area,
		LOC(2, 2),
		LOC(2, 3),
		LOC(2, 4),
		LOC(3, 2),
		LOC(3, 4),
		LOC(4, 2),
		LOC(4, 3),
		LOC(4, 4),
	)

	var/list/contained_range_case = list(
		contained_trash_1,
		contained_trash_2,
		contained_trash_3,
		container,
		contained,
	)

	var/list/contained_orange_case = list(
		container,
	)


	run_case(center, turf_range_case, nameof(turf_range_case), FALSE)
	run_case(center, turf_orange_case, nameof(turf_orange_case), TRUE)
	run_case(container, container_range_case, nameof(container_range_case), FALSE)
	run_case(container, container_orange_case, nameof(container_orange_case), TRUE)
	run_case(contained, contained_range_case, nameof(contained_range_case), FALSE)
	run_case(contained, contained_orange_case, nameof(contained_orange_case), TRUE)

	// unusual cases

	var/list/turf_range_2x2 = list(
		center,
		outer_area,
		container,
		LOC(2, 2),
		LOC(2, 3),
		LOC(3, 2),
	)

	var/list/turf_orange_2x2 = list(
		LOC(2, 2),
		outer_area,
		LOC(2, 3),
		LOC(3, 2),
	)

	var/list/turf_range_1x3 = list(
		center,
		outer_area,
		container,
		LOC(3, 2),
		LOC(3, 4),
	)

	var/list/turf_orange_1x3 = list(
		LOC(3, 2),
		outer_area,
		LOC(3, 4)
	)

	var/list/turf_range_3x1 = list(
		center,
		outer_area,
		container,
		LOC(2, 3),
		LOC(4, 3),
	)

	var/list/turf_orange_3x1 = list(
		LOC(2, 3),
		outer_area,
		LOC(4, 3),
	)

	run_case(center, turf_range_2x2, nameof(turf_range_2x2), FALSE, "2x2")
	run_case(center, turf_orange_2x2, nameof(turf_orange_2x2), TRUE, "2x2")
	run_case(center, turf_range_1x3, nameof(turf_range_1x3), FALSE, "1x3")
	run_case(center, turf_orange_1x3, nameof(turf_orange_1x3), TRUE, "1x3")
	run_case(center, turf_range_3x1, nameof(turf_range_3x1), FALSE, "3x1")
	run_case(center, turf_orange_3x1, nameof(turf_orange_3x1), TRUE, "3x1")

	// FIXME: these pass in BYOND, but the way we iterate over area turfs diverges
	// var/list/area_range_case = list(outer_area) + outer_area.contents
	// var/list/area_orange_case = area_range_case.Copy()
	// run_case(outer_area, area_range_case, nameof(area_range_case), FALSE)
	// run_case(outer_area, area_orange_case, nameof(area_orange_case), TRUE)

	del(container)
	
#undef LOC
