

/proc/RunTest()
	var/text = "["1"]\s"
	ASSERT(text == "1s")
	text = "[0]\s"
	ASSERT(text == "0s")
	text = "[null]\s"
	ASSERT(text == "s")
	text = "[1]\s"
	ASSERT(text == "1")
	text = "[1.00000001]\s"
	ASSERT(text == "1")
	text = "[1.0001]\s"
	ASSERT(text == "1.0001s")
