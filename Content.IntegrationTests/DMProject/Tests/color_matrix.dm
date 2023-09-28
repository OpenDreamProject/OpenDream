/proc/test_color_matrix()
	var/r = "#de000000"
	var/g = "#00ad0000"
	var/b = "#0000be00"
	var/a = "#000000ef" // deadbeef my beloved
	var/mob/M = new
	M.color = list(r,g,b,a)
	if(M.color != "#deadbe")
		CRASH("Color matrix transformation in rgba() value didn't work correctly, color is '[json_encode(M.color)]' instead.")
	M.color = null
	M.filters += filter(type="color",color=list(r,g,b,a))
	if(M.filters.len < 1)
		CRASH("Using a color filter created using filter() was not successful.")