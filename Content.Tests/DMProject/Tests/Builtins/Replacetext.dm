
/proc/RunTest()
	var/str = "ABCDEF"

	// Replacing the first char. Start=2 ignores the first char.
	ASSERT(replacetext(str, "A", "G") == "GBCDEF")
	ASSERT(replacetext(str, "A", "G", 1) == "GBCDEF")
	ASSERT(replacetext(str, "A", "G", 2) == "ABCDEF")
	ASSERT(replacetext(str, "A", "G", -5) == "ABCDEF")
	ASSERT(replacetext(str, "A", "G", -6) == "GBCDEF")
	ASSERT(replacetext(str, "A", "G", -100) == "GBCDEF")

	// Replacing the last char. End=6 ignores the last char.
	ASSERT(replacetext(str, "F", "G") == "ABCDEG")
	ASSERT(replacetext(str, "F", "G", 1, 7) == "ABCDEG")
	ASSERT(replacetext(str, "F", "G", 1, 6) == "ABCDEF")

	// A null Needle causes the Replacement to be placed after every character but the last
	ASSERT(replacetext(str, null, "G") == "AGBGCGDGEGF")
	ASSERT(replacetext(str, null, "HI") == "AHIBHICHIDHIEHIF")

	// Start=1 and Start=2 are the same (but only with null Needle)
	ASSERT(replacetext(str, null, "G", 1) == "AGBGCGDGEGF")
	ASSERT(replacetext(str, null, "G", 2) == "AGBGCGDGEGF")
	ASSERT(replacetext(str, null, "G", 3) == "ABGCGDGEGF")
	ASSERT(replacetext(str, null, "G", 4) == "ABCGDGEGF")
	ASSERT(replacetext(str, null, "G", 1, 4) == "AGBGCGDEF")
	ASSERT(replacetext(str, null, "G", 1, 5) == "AGBGCGDGEF")

	// An End reaching past the end of the string still won't place the Replacement at the end
	ASSERT(replacetext(str, null, "G", 1, 6) == "AGBGCGDGEGF")
	ASSERT(replacetext(str, null, "G", 1, 7) == "AGBGCGDGEGF")

	// End wraps around at <= 0
	ASSERT(replacetext(str, "E", "G", 1, 0) == "ABCDGF")
	ASSERT(replacetext(str, "E", "G", 1, -1) == "ABCDGF")
	ASSERT(replacetext(str, "E", "G", 1, -2) == "ABCDEF")
	ASSERT(replacetext(str, "E", "G", 1, -200) == "ABCDEF")

	// /regex can be used as a Needle
	ASSERT(replacetext(str, new /regex("."), "G") == "GBCDEF");
	ASSERT(replacetext(str, new /regex(".", "g"), "G") == "GGGGGG");
