/datum/version
	var/version
	var/build

var/const/lib = "E:/Projects/OpenDream/bin/Content.Tests/byondapitest.dll"

/proc/RunTest()
	var/datum/version/v = new
	var/result = call_ext(lib, "byond:echo_get_version")(v)
	world.log << "Real: [result]"
