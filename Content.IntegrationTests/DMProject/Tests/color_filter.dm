/datum/unit_test/color_filter/RunTest()
	var/r = "#de000000"
	var/g = "#00ad0000"
	var/b = "#0000be00"
	var/a = "#000000ef" // deadbeef my beloved
	var/mob/M = new
	M.color = null
	M.filters = null
	M.filters += filter(type="color",color=list(r,g,b,a))
	if(M.filters.len < 1)
		CRASH("Using a color filter created using filter() was not successful.")