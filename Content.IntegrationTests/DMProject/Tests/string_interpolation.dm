/obj/blombo
	name = "Blombo"
	gender = FEMALE

/obj/blorpo
	name = "Blorpo"
	gender = MALE

/proc/test_string_interpolation()
	var/obj/blombo/b = new
	var/obj/blorpo/b2 = new
	var/result_text = "[b]? Nobody likes \him. \He is awful! Unlike [b2]. \He is pretty cool!"
	ASSERT(result_text == "Blombo? Nobody likes her. She is awful! Unlike Blorpo. He is pretty cool!")