/proc/RunTest()

	// MinDigits / Radix
	ASSERT(num2text(999.23123, 4, 10) == "0999")
	ASSERT(num2text(999.23123, 4, 12) == "06b3")
	ASSERT(num2text(999.23123, 4, 3) == "1101000")
	ASSERT(num2text(999.23123, 20, 5) == "00000000000000012444")
	ASSERT(num2text(20, 0, 16) == "14")
	ASSERT(num2text(-20, 0, 16) == "-14")

	// Sigfigs
	ASSERT(num2text(999.23123, 2) == "1e+03")
	ASSERT(num2text(999.23123, 25) == "999.231201171875")

	// General Formatting
	ASSERT(num2text(999999) == "999999")
	ASSERT(num2text(999999.9) == "1e+06")
	ASSERT(num2text(1000000) == "1e+06")
	
	// Zero/Negative MinDigits
	ASSERT(num2text(1, 0, 10) == "1")
	ASSERT(num2text(1, -1, 10) == "1")
	ASSERT(num2text(0, 0, 16) == "0")