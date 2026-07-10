// Assumes /icon.GetPixel(x, y) is working
// Assumes splittext(string, delimiter) is working

/* 
	=== Making a test icon ===
	The pattern for a test case is like so: color,x1,y1,x2,y2
	Empty parameters will be converted into null, the only thing important
	Examples:
	- Make a fully red icon: red,1,1,32,32
	- Put a dot in the center: red,15,15,,
	- Empty call: ,,,,

	Operations can be chained together with ";" as a separator
	Examples:
	- Make a red icon, then cut a hole in the center: red,1,1,32,32;,8,8,25,25
	- Make a red-green checkerboard: red,1,1,16,16;green,1,17,16,32;red,17,17,32,32;green,17,1,32,16
*/

/proc/CompareIcons(operation, icon/generated, icon/expected)
	for(var/y in 1 to expected.Height())
		for(var/x in 1 to expected.Width())
			if(generated.GetPixel(x, y) != expected.GetPixel(x, y))
				throw EXCEPTION("[operation] was not equivalent")

/proc/to_args(text)
	. = splittext(text, ",")
	for(var/i in 2 to length(.))
		.[i] = text2num(.[i])

/proc/RunTest()
	var/static/list/ignored_states = list()

	var/static/icon/results_icon = icon('expected_results/DrawBox.dmi')
	var/static/icon/base_icon = icon(results_icon, "")

	for(var/state in icon_states(results_icon, 1) - "")
		var/icon/expected_icon = icon('expected_results/DrawBox.dmi', state) // FIXME: passing results_icon doesn't work here
		var/icon/generated_icon = icon(base_icon)
		for(var/operation in splittext(state, ";"))
			generated_icon.DrawBox(arglist(to_args(operation)))
		CompareIcons(state, generated_icon, expected_icon)