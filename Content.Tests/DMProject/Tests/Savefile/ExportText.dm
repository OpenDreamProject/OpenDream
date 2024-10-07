/datum/savetest
	var/name
	var/datum/savetest/recurse = null

/proc/RunTest()
	var/datum/savetest/O = new() //create a test object
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

	ASSERT(F.dir ~= list("dir","dir2","dir3","dir4","dir5","dir6","dir7","dirIcon","list"))
	ASSERT(F.ExportText("dir6/subdir6") == ". = 321\n")
	ASSERT(F.ExportText("dir6/subdir6/") == ". = 321\n")
	ASSERT(F.ExportText("dir6") == "\nsubdir6 = 321\n")
	var/list_match = {". = list("1",2,"three" = 3,4,object(".0"),object(".1"),list(1,2,3,object(".2")))\n.0\n\ttype = /datum\n.1\n\ttype = /datum\n.2\n\ttype = /datum\n"}
	ASSERT(F.ExportText("list") == list_match)

	F.cd = "dir6"
	F << "test"
	F.cd = "/"
	ASSERT(F.ExportText("dir6") == ". = \"test\"\nsubdir6 = 321\n")
	ASSERT(F.ExportText("dir6/subdir6/") == ". = 321\n")