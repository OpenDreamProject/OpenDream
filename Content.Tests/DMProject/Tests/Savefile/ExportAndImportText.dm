/obj/savetest
	var/obj/savetest/recurse = null

/proc/RunTest()
	var/obj/savetest/O = new() //create a test object
	O.name = "test"
	//O.recurse = O //TODO

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
	F["list"] << list("1",2,"three"=3,4, new /datum(), new /datum(), list(1,2,3, new /datum()))

	ASSERT(F.ExportText("dir6/subdir6") == ". = 321\n")
	ASSERT(F.ExportText("dir6/subdir6/") == ". = 321\n")
	ASSERT(F.ExportText("dir6") == "\nsubdir6 = 321\n")
	var/list_match = @{". = list("1",2,"three" = 3,4,object(".0"),object(".1"),list(1,2,3,object(".2")))
.0
    type = /datum
.1
    type = /datum
.2
    type = /datum"}
	ASSERT(F.ExportText("list") == list_match)


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