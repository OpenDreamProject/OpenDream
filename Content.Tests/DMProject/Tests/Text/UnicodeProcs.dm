/proc/RunTest()
	ASSERT(length("😀") == 2)
	ASSERT(length_char("😀") == 1)

	// This is the combination of the Man, Woman, Girl and Boy emojis.
	// It's 1 character per emoji, plus 3 zero-width joiner characters between them.
	ASSERT(length("👨‍👩‍👧‍👦") == 11)
	ASSERT(length_char("👨‍👩‍👧‍👦") == 7)