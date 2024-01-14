/obj/savetest
	var/obj/savetest/recurse = null
	New(args)
		proc_call_order_check += list("New")
		..()

	Read(savefile/F)
		proc_call_order_check += list("Read")
		..()

	Write(savefile/F)
		proc_call_order_check += list("Write")
		..()

/var/static/proc_call_order_check = list()

/proc/RunTest()
	var/obj/savetest/O = new() //create a test object
	O.name = "test"
	O.recurse = O

	var/savefile/F = new() //create a temporary savefile

	F["dir"] = O
	F["dir2"] = "object(\".0\")"
	F["dir3"] = 1080
	F["dir4"] = "the afternoon of the 3rd"
	var/savefile/P = new() //nested savefile
	P["subsavedir1"] = O
	P["subsavedir2"] = "butts"
	P["subsavedir3"] = 123
	
	F["dir5"] = P
	F["dir6/subdir6"] = 321
	F["dir7"] = null
	F["dirIcon"] = new /icon()

	ASSERT(F.ExportText("dir6/subdir6") == ". = 321\n")
	ASSERT(F.ExportText("dir6/subdir6/") == ". = 321\n")
	ASSERT(F.ExportText("dir6") == "\nsubdir6 = 321\n")


	var/import_test = @{"
dir1 = 1080
dir2 = "object(\".0\")"
dir4 = "the afternoon of the 3rd"
dir6
	subdir6 = 321
		subsubdir
			key = "value"
dir7 = null
"}

	var/savefile/F2 = new()
	F2.ImportText("/",import_test)
	world.log << F2.ExportText()
	ASSERT(F2["dir1"] == 1080)
	ASSERT(F2["dir2"] == "object(\".0\")")
	ASSERT(F2["dir4"] == "the afternoon of the 3rd")
	ASSERT(F2["dir6/subdir6"] == 321)
	ASSERT(F2["dir6/subdir6/subsubdir/key"] == "value")
	ASSERT(F2["dir7"] == null)