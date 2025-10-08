//RUNTIME ERROR, RETURN TRUE
var/error_loop_count = 0

/world/Error()
	error_loop_count++
	if(error_loop_count > 1)
		world.log << "this is a test failure"
		return
	SubError()	

/proc/SubError()
	CRASH("error handling error")

/proc/TestProc()
	CRASH("oh no, an error")

/proc/RunTest()
	TestProc()
	return error_loop_count == 1