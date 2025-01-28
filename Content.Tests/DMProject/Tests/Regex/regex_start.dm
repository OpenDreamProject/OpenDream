/proc/bar()
	. = "bar"

/proc/RunTest()
	var/regex/R = regex(@"foo")
	var/result = R.Replace("foo foo", "bar", start=1)
	ASSERT(result == "bar foo")

	result = R.Replace("foo foo", "bar", start=2)
	ASSERT(result == "foo bar")

	result = R.Replace("foo foo", "bar", 2)
	ASSERT(result == "foo bar")

	result = R.Replace("foo foo", /proc/bar, start=2)
	ASSERT(result == "foo bar")

	result = R.Replace("foo foo", "bar", start=5)
	ASSERT(result == "foo bar")

	result = R.Replace("foo foo", "bar", start=6)
	ASSERT(result == "foo foo")

	result = R.Replace("foo foo", "bar", start=420)
	ASSERT(result == "foo foo")

	ASSERT(R.Find("foo foo", start=1) == 1)
	ASSERT(findtext("foo foo", R, Start=1) == 1)

	ASSERT(R.Find("foo foo", start=2) == 5)
	ASSERT(findtext("foo foo", R, Start=2) == 5)

	ASSERT(R.Find("foo foo", start=5) == 5)
	ASSERT(findtext("foo foo", R, Start=5) == 5)

	ASSERT(R.Find("foo foo", start=6) == 0)
	ASSERT(findtext("foo foo", R, Start=6) == 0)

	ASSERT(R.Find("foo foo", start=69) == 0)
	ASSERT(findtext("foo foo", R, Start=69) == 0)
