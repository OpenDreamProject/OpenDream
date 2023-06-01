/proc/RunTest()
	var/resource = 'data/test.txt'
	ASSERT(file2text(resource) == "Test resource file's content")
