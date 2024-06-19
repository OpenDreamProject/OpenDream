/proc/RunTest()
	var/resource = 'data/test.txt'
	ASSERT(file2text(resource) == "Test resource file's content")

	// Compile-time resources always use a forward slash
	// file() does not
	ASSERT("['data/test.txt']" == "data/test.txt")
	ASSERT("['data\\test.txt']" == "data/test.txt")
	ASSERT("['./data/test.txt']" == "data/test.txt")
	ASSERT("['.\\data\\test.txt']" == "data/test.txt")
	ASSERT("[file("data/test.txt")]" == "data/test.txt")
	ASSERT("[file("./data/test.txt")]" == "./data/test.txt")
	ASSERT("[file("data\\test.txt")]" == "data\\test.txt") // Note the backslash here
	ASSERT("[file(".\\data\\test.txt")]" == ".\\data\\test.txt") // And here
