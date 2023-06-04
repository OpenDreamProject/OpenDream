/proc/RunTest()
	var/regex/R = regex(@"foo", "g")
	var/text = "foo foo foo foo"

	ASSERT(R.Find(text) == 1)
	ASSERT(R.next == 4)
	ASSERT(R.Find(text) == 5)
	ASSERT(R.next == 8)
	ASSERT(R.Find(text) == 9)
	ASSERT(R.next == 12)
	ASSERT(R.Find(text) == 13)
	ASSERT(R.next == 16)
	ASSERT(R.Find(text) == 0)
	ASSERT(isnull(R.next))
	ASSERT(R.Find(text) == 1)
	ASSERT(R.next == 4)

	var/text2 = "foo foo"
	ASSERT(R.Find(text2) == 1)
	ASSERT(R.next == 4)
	ASSERT(R.Find(text2) == 5)
	ASSERT(R.next == 8)

