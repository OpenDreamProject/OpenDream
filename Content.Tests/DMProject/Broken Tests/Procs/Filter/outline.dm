//This test actually passes in the client, but breaks in testing due to UpdateAppearance() not working in the test environment.
/proc/RunTest()	
	var/obj/thing = new()
	thing.filters = filter(type="outline", size=1, color=rgb(255,0,0))
	var/ex_filter = thing.filters[1]
	ex_filter:size = 2
	ASSERT(thing.filters[1]:size == 2)