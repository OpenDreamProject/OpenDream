
/datum/blombo
	var/name = "Blombo"

/proc/RunTest()
	var/datum/blombo/b = new
	var/result_text = "Nobody likes [b]!"
	ASSERT(result_text == "Nobody likes Blombo!")
