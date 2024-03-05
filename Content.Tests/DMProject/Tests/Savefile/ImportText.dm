/proc/RunTest()
	var/savefile/S = new()
	// Uncomment once issue 1707 is resolved
	/*S.ImportText("/", "basefoldercheck")
	ASSERT("basefoldercheck" in S)

	S.ImportText("/", "subdirectory/insert")
	ASSERT(!("insert" in S))
	ASSERT("subdirectory" in S)

	S.ImportText("/subdirectory", "insert2")
	ASSERT(!("insert2" in S))*/

	var/savefile/S2 = new()
	S2.ImportText("/", "importexport")
	ASSERT(S2.ExportText("/") == "\nimportexport\n")

	S.ImportText("/", "key = value")
	ASSERT(S["key"] == "value")

	// See previous comment
	/*S.ImportText("/", "concurrent_entry1;concurrent_entry2")
	ASSERT("concurrent_entry1" in S)
	ASSERT("concurrent_entry2" in S)*/

	S.ImportText("/", "concurrent_keyvalue1 = 5;concurrent_keyvalue2 = 10")
	ASSERT(S["concurrent_keyvalue1"] == 5)
	ASSERT(S["concurrent_keyvalue2"] == 10)

	S.ImportText("/", file("ImportTextFile.txt"))
	ASSERT(S["textfile_import_test"] == 2)
