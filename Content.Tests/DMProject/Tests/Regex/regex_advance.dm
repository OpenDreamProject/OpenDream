/proc/RunTest()
	var/regex/R = regex(@"foo", "g")
	var/text = "foo foo foo foo"

	ASSERT(R.Find(text) == 1)
	ASSERT(R.Find(text) == 5)
	ASSERT(R.Find(text) == 9)
	ASSERT(R.Find(text) == 13)
	ASSERT(R.Find(text) == 0)

