/proc/regex_callback(match, group1, group2)
	. = "good"

	// The proc should return back to regex/Replace here despite being not being a `waitfor=0` proc
	sleep(1)

	. = "bad"

/proc/RunTest()
	var/regex/R = regex(@"\w+")
	var/result = R.Replace("Hello, there", /proc/regex_callback)
	ASSERT(result == "good, there")
	ASSERT(R.next == 5)