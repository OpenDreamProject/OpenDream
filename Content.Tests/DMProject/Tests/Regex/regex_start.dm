/proc/bar()
	. = "bar"

/proc/RunTest()
	var/regex/R = regex(@"foo")
	var/result = R.Replace("foo foo", "bar", 1)
	ASSERT(result == "bar foo")

	result = R.Replace("foo foo", "bar", 2)
	ASSERT(result == "foo bar")

	result = R.Replace("foo foo", "bar", 2)
	ASSERT(result == "foo bar")

	result = R.Replace("foo foo", /proc/bar, 2)
	ASSERT(result == "foo bar")

	result = R.Replace("foo foo", "bar", 5)
	ASSERT(result == "foo bar")

	result = R.Replace("foo foo", "bar", 6)
	ASSERT(result == "foo foo")

	result = R.Replace("foo foo", "bar", 420)
	ASSERT(result == "foo foo")

	ASSERT(R.Find("foo foo", 1) == 1)
	ASSERT(findtext("foo foo", R, 1) == 1)

	ASSERT(R.Find("foo foo", 2) == 5)
	ASSERT(findtext("foo foo", R, 2) == 5)

	ASSERT(R.Find("foo foo", 5) == 5)
	ASSERT(findtext("foo foo", R, 5) == 5)

	ASSERT(R.Find("foo foo", 6) == 0)
	ASSERT(findtext("foo foo", R, 6) == 0)

	ASSERT(R.Find("foo foo", 69) == 0)
	ASSERT(findtext("foo foo", R, 69) == 0)

#ifdef OPENDREAM
	// BYOND doesn't support named params for some builtins because it's terrible
	// just keep this here to check for regression
	ASSERT(R.Find("foo foo", start=1) == 1)
	ASSERT(findtext("foo foo", R, Start=1) == 1)

	ASSERT(R.Find("foo foo", start=2) == 5)
	ASSERT(findtext("foo foo", R, Start=2) == 5)
#endif