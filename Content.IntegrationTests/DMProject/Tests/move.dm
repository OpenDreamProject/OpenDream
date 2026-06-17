// Ensures movable.Move() has the correct order and iteration

var/global/testing_move = FALSE
var/global/list/move_trace = list()
#define LOG_LOC(some_loc) (isturf(some_loc) ? "[(some_loc).type] ([(some_loc).x],[(some_loc).y])" : "[(some_loc).type] [(some_loc).name]")
#define BEGIN_TRACE(separator) move_trace.Cut()
#define MARK(ref, mark) "[LOG_LOC(ref)] [mark]"
#define TRACE_STEP(ref, mark) if(testing_move){move_trace += MARK(ref, mark)}

#define ENTER "enter"
#define ENTERED "entered"
#define EXIT "exit"
#define EXITED "exited"
#define CROSS "cross"
#define CROSSED "crossed"
#define UNCROSS "uncross"
#define UNCROSSED "uncrossed"
#define BUMP "bump"
#define MOVE "move"

/atom/Enter(O, oldloc)
	TRACE_STEP(src, ENTER)
	. = ..()

/atom/Entered(Obj, OldLoc)
	TRACE_STEP(src, ENTERED)
	. = ..()

/atom/Exit(O, newloc)
	TRACE_STEP(src, EXIT)
	. = ..()

/atom/Exited(Obj, newloc)
	TRACE_STEP(src, EXITED)
	. = ..()

/atom/Cross(O)
	TRACE_STEP(src, CROSS)
	. = ..()

/atom/Crossed(O)
	TRACE_STEP(src, CROSSED)
	. = ..()

/atom/Uncross(O)
	TRACE_STEP(src, UNCROSS)
	. = ..()

/atom/Uncrossed(O)
	TRACE_STEP(src, UNCROSSED)	
	. = ..()

/atom/movable/Bump(Obstacle)
	TRACE_STEP(src, BUMP)
	. = ..()

/atom/movable/Move(NewLoc, Dir, step_x, step_y)
	TRACE_STEP(src, MOVE)
	. = ..()

#define RUN_TEST(expected, target, direction) _run_test(expected, nameof(expected), target, direction)
/datum/unit_test/move_parity/proc/_run_test(list/expected, identifier, atom/movable/target, direction)
	var/error_index = 1
	try
		BEGIN_TRACE("running [identifier]")
		step(target, direction)
		
		if(expected.len != move_trace.len)
			error_index = expected.len
			CRASH("result was [expected.len > move_trace.len ? "shorter" : "longer"] than expected")
		
		for(var/i in 1 to expected.len)
			if(expected[i] != move_trace[i])
				error_index = i
				CRASH("expected did not match result")
	catch(var/exception/exc)
		var/list/error_message = list("[identifier]: [exc]")

		error_message += "expected:"
		for(var/i in 1 to expected.len)
			error_message += "\t[expected[i]][i == error_index ? "<--- here" : null]"

		error_message += "got:"
		for(var/i in 1 to move_trace.len)
			error_message += "\t[move_trace[i]][i == error_index ? "<--- here" : null]"

		CRASH(error_message.Join("\n"))

// this needs to be a proc cause BYOND gets confused 
/datum/unit_test/move_parity/proc/LOC(x, y) as /turf
	return locate(x, y, 3)

/datum/unit_test/move_parity/RunTest()
	testing_move = TRUE
	world.maxx = world.maxy = 2
	var/mob/protag = new(LOC(1, 1))
	protag.name = "protag"
	var/list/protag_init = list()
	RUN_TEST(protag_init, protag, 0)

	var/list/protag_step = list(
		MARK(protag, MOVE),
		MARK(LOC(1, 1), EXIT),
		MARK(LOC(1, 1), UNCROSS),
		MARK(LOC(2, 1), ENTER),
		MARK(LOC(2, 1), CROSS),
		MARK(LOC(1, 1), EXITED),
		MARK(LOC(1, 1), UNCROSSED),
		MARK(LOC(2, 1), ENTERED),
		MARK(LOC(2, 1), CROSSED),
	)
	RUN_TEST(protag_step, protag, EAST)

	var/list/protag_wall = list()
	RUN_TEST(protag_wall, protag, EAST)

	var/mob/antag = new(LOC(1, 1))
	antag.name = "antag"
	var/list/protag_bump = list(
		MARK(protag, MOVE),
		MARK(LOC(2, 1), EXIT),
		MARK(LOC(2, 1), UNCROSS),
		MARK(LOC(1, 1), ENTER),
		MARK(LOC(1, 1), CROSS),
		MARK(antag, CROSS),
		MARK(protag, BUMP),
	)
	RUN_TEST(protag_bump, protag, WEST)

	var/mob/deuterag = new(LOC(2, 2))
	deuterag.name = "deuterag"
	deuterag.density = FALSE
	var/list/protag_cross = list(
		MARK(protag, MOVE),
		MARK(LOC(2, 1), EXIT),
		MARK(LOC(2, 1), UNCROSS),
		MARK(LOC(2, 2), ENTER),
		MARK(LOC(2, 2), CROSS),
		MARK(deuterag, CROSS),
		MARK(LOC(2, 1), EXITED),
		MARK(LOC(2, 1), UNCROSSED),
		MARK(LOC(2, 2), ENTERED),
		MARK(LOC(2, 2), CROSSED),
		MARK(deuterag, CROSSED),
	)
	RUN_TEST(protag_cross, protag, NORTH)

	var/list/protag_uncross = list(
		MARK(protag, MOVE),
		MARK(LOC(2, 2), EXIT),
		MARK(LOC(2, 2), UNCROSS),
		MARK(deuterag, UNCROSS),
		MARK(LOC(2, 1), ENTER),
		MARK(LOC(2, 1), CROSS),
		MARK(LOC(2, 2), EXITED),
		MARK(LOC(2, 2), UNCROSSED),
		MARK(deuterag, UNCROSSED),
		MARK(LOC(2, 1), ENTERED),
		MARK(LOC(2, 1), CROSSED),
	)
	RUN_TEST(protag_uncross, protag, SOUTH)

	del(deuterag)
	del(antag)
	del(protag)

/datum/unit_test/move_parity/Del()
	testing_move = FALSE	

#undef RUN_TEST
#undef LOG_LOC
#undef BEGIN_TRACE
#undef MARK
#undef TRACE_STEP
#undef ENTER
#undef ENTERED
#undef EXIT
#undef EXITED
#undef CROSS
#undef CROSSED
#undef UNCROSS
#undef UNCROSSED
#undef BUMP
#undef MOVE