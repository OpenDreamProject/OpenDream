//RUNTIME ERROR, RETURN TRUE
var/error_loop_count = 0

/world/Error()
	error_loop_count++
	if(error_loop_count > 1)
		world.log << "this is a test failure"
		return
	TestProc()	

/proc/SubError()
	CRASH("we're doing an error!")

/proc/TestProc()
	SubError()

/proc/RunTest()
	TestProc()
	return error_loop_count == 1