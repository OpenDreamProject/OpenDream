/proc/RunTest()
	ASSERT(length("😀") == 4)
	ASSERT(length_char("😀") == 1)

	// This is the combination of the Man, Woman, Girl and Boy emojis.
	// It's 1 character per emoji, plus 3 zero-width joiner characters between them.
	ASSERT(length("👨‍👩‍👧‍👦") == 25)
	ASSERT(length_char("👨‍👩‍👧‍👦") == 7)