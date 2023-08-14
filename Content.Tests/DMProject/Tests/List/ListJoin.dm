/proc/RunTest()
	var/list/L = list(1, 2, 3, 4, 5)

	ASSERT(L.Join(";") == "1;2;3;4;5")
	ASSERT(L.Join("_") == "1_2_3_4_5")
	ASSERT(L.Join(null) == "12345")
	ASSERT(L.Join(1) == "12345")

	ASSERT(L.Join(";", 2) == "2;3;4;5")
	ASSERT(L.Join(";", 4) == "4;5")
	ASSERT(L.Join(";", 0) == "")
	ASSERT(L.Join(";", -3) == "3;4;5")
	ASSERT(L.Join(";", new /datum()) == "1;2;3;4;5")

	ASSERT(L.Join(";", 1, 0) == "1;2;3;4;5")
	ASSERT(L.Join(";", -4, -2) == "2;3")


