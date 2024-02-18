/proc/RunTest()
	ASSERT(text2ascii_char("abcdéfg",6) == 102) //102 is f
	ASSERT(ascii2text(text2ascii("a")) == "a")
	var/list/values = list(
		"©" = 0x00A9,
		"®" = 0x00AE,
		"‼" = 0x203C,
		"⁉" = 0x2049,
		"⃣" = 0x20E3,
		"™" = 0x2122,
		"ℹ" = 0x2139,
		"⌚" = 0x231A,
		"⌛" = 0x231B,
		"⌨" = 0x2328,
		"⏏" = 0x23CF,
		"Ⓜ" = 0x24C2,
		"▪" = 0x25AA,
		"▫" = 0x25AB,
		"▶" = 0x25B6,
		"◀" = 0x25C0,
		"⤴" = 0x2934,
		"⤵" = 0x2935,
		"〰" = 0x3030,
		"〽" = 0x303D,
		"㊗" = 0x3297,
		"㊙" = 0x3299,
	)
	for(var/v in values)
		log << "[v]([values[v]]) == [ascii2text(values[v])]\n"
		ASSERT(ascii2text(values[v]) == v)