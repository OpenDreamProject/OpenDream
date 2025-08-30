/obj/thingtocopy
	name = "hello"
	desc = "this is a thing"

/datum/unit_test/atom_appearance/RunTest()
	var/obj/thingtocopy/T = new()
	var/obj/otherthing = new()
	otherthing.appearance = T.appearance
	ASSERT(otherthing.name == T.name)
	ASSERT(otherthing.desc == T.desc)