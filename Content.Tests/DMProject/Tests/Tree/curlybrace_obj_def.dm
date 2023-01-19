/mob/testmob{name="thing"; color="#ff0000"}

/proc/RunTest()
	var/mob/testmob/T = new
	ASSERT(T.name == "thing")
	ASSERT(T.color == "#ff0000")