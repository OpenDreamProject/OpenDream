var/i = 1

/proc/counter(x)
	. = "[i++]"

/proc/bar()
	. = "bar"

/proc/RunTest()
	var/regex/R = regex(@"foo", "g")
	ASSERT(R.Replace("foo foo foo", /proc/bar) == "bar bar bar")
	ASSERT(R.Replace("foo foo foo", /proc/counter) == "1 2 3")
	ASSERT(R.Replace("foofoofoo", /proc/counter) == "456")

	R = regex(@"ba", "g")
	ASSERT(R.Replace("baa ba", "b") == "ba b")
	
	R = regex(@"foo") // not global
	ASSERT(R.Replace("foo foo foo", /proc/bar) == "bar foo foo")
