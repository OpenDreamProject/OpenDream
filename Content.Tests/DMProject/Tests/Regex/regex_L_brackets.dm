
/proc/RunTest()
	var/static/regex/repeated_consonant_regex = regex(@"\b([^aeiou\L])\1", "gi")
	var/words = "hey ggggurl"
	ASSERT(repeated_consonant_regex.Find(words) != 0)
