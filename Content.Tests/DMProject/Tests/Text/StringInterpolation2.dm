
/obj/blombo
	name = "Blombo"

/proc/RunTest()
	var/obj/blombo/b = new
	var/result_text = "Nobody likes [b]!"
	ASSERT(result_text == "Nobody likes Blombo!")
