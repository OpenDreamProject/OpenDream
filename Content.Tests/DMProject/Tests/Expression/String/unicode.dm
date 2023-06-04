// issue #1239

/proc/RunTest()
	var/in1 = "\x55"
	var/out1 = "U"
	ASSERT(in1 == out1)

	var/in2 = "\u1155"
	var/out2 = "ᅕ"
	ASSERT(in2 == out2)

	var/in3 = "\u123456"
	var/out3 = "ሴ56"
	ASSERT(in3 == out3)
