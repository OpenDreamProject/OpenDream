

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
	text = "[4.52]\th"
	ASSERT(text == "4th")
	text = "the fitness [1.7]\th is a"
	ASSERT(text == "the fitness 1st is a")
	text = "the fitness [99999999]\th is a"
	ASSERT(text == "the fitness 100000000th is a")
	// TODO: this should assert/eval to 0th
	text = "[null]\th"
	ASSERT(findtextEx(text,"th") != 0)

