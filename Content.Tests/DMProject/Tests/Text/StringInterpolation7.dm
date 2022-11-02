

/proc/RunTest()
	var/text = "[0]\th"
	ASSERT(text == "0th")
	text = "[1]\th"
	ASSERT(text == "1st")
	text = "[2]\th"
	ASSERT(text == "2nd")
	text = "[3]\th"
	ASSERT(text == "3rd")
	text = "[4]\th"
	ASSERT(text == "4th")
	text = "[-1]\th"
	ASSERT(text == "-1th")
	// TODO: this should assert/eval to 0th
	text = "[null]\th"
	ASSERT(findtextEx(text,"th") != 0)

