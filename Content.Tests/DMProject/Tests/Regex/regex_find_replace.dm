var/models = "/obj/cable/brown{\n\ticon_state = \"2-8\"\n\t},\n/obj/cable/brown{\n\ticon_state = \"4-8\"\n\t},\n/turf/simulated/floor/orangeblack,\n/area/station/devzone"
var/result_match = "/obj/cable/brown{\n\ticon_state = \"1\"\n\t},\n/obj/cable/brown{\n\ticon_state = \"2\"\n\t},\n/turf/simulated/floor/orangeblack,\n/area/station/devzone"

/proc/RunTest()
	var/list/originalStrings = list()
	var/regex/noStrings = regex(@{"(["])(?:(?=(\\?))\2(.|\n))*?\1"})
	var/stringIndex = 1
	var/found
	do
		found = noStrings.Find(models, noStrings.next)
		if(found)
			var indexText = {""[stringIndex]""}
			stringIndex++
			var match = copytext(noStrings.match, 2, -1) // Strip quotes
			models = noStrings.Replace(models, indexText, found)
			originalStrings[indexText] = (match)
	while(found)
	ASSERT(models == result_match)