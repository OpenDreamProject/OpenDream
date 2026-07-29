/proc/RunTest()
	var/text_value = "bad"
	ASSERT(++text_value == 1)

	var/list/list_values = list("a","b","c")
	ASSERT(++list_values[1] == 1)

	var/null_value = null
	ASSERT(++null_value == 1)
