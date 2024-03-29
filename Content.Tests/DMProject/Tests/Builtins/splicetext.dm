/proc/RunTest()
    ASSERT(splicetext("banana", 3, 6, "laclav") == "balaclava")
    ASSERT(splicetext("banana", 1, 6, "laclav") == "laclava")
    ASSERT(splicetext("banana", 3, 0, "laclav") == "balaclav")
    ASSERT(splicetext("abcdef", 3) == "ab")
    ASSERT(splicetext("abcdef", 3, 5) == "abef")
    ASSERT(splicetext("abcdef", 3, 5, null) == "abef")
    ASSERT(splicetext("abcdef", -2, 5, "test") == "abcdtestef")
    ASSERT(splicetext("abcdef", -2, 7, "test") == "abcdtest")
    ASSERT(splicetext("abcdef", 3, -1, "test") == "abtestf")
    ASSERT(splicetext("abcdef", 2, 33, "test") == "atest")
    ASSERT(splicetext("abcdef", 2, -1, "test") == "atestf")
    ASSERT(splicetext(null, 4, 5) == null)
    ASSERT(splicetext("", 4, 5) == "")
    ASSERT(splicetext(null, 4, 5, "test") == "test")
    ASSERT(splicetext("", 4, 5, "test") == "test")
    //splicetext_char
    ASSERT(splicetext_char("𝖇𝖆𝖓𝖆𝖓𝖆", 3, 6, "𝓵𝓪𝓬𝓵𝓪𝓿") == "𝖇𝖆𝓵𝓪𝓬𝓵𝓪𝓿𝖆")
    //ASSERT(splicetext("𝖇𝖆𝖓𝖆𝖓𝖆", 3, 6, "𝓵𝓪𝓬𝓵𝓪𝓿") == "𝓵𝓪𝓬𝓵𝓪𝓿𝖓𝖆𝖓𝖆") //not compatible with BYOND
    ASSERT(splicetext_char("banana", 3, 6, "laclav") == "balaclava")
    ASSERT(splicetext_char("banana", 1, 6, "laclav") == "laclava")
    ASSERT(splicetext_char("banana", 3, 0, "laclav") == "balaclav")
    ASSERT(splicetext_char("abcdef", 3) == "ab")
    ASSERT(splicetext_char("abcdef", 3, 5) == "abef")
    ASSERT(splicetext_char("abcdef", 3, 5, null) == "abef")
    ASSERT(splicetext_char("abcdef", -2, 5, "test") == "abcdtestef")
    ASSERT(splicetext_char("abcdef", -2, 7, "test") == "abcdtest")
    ASSERT(splicetext_char("abcdef", 3, -1, "test") == "abtestf")
    ASSERT(splicetext_char("abcdef", 2, 33, "test") == "atest")
    ASSERT(splicetext_char("abcdef", 2, -1, "test") == "atestf")
    ASSERT(splicetext_char(null, 4, 5) == null)
    ASSERT(splicetext_char("", 4, 5) == "")
    ASSERT(splicetext_char(null, 4, 5, "test") == "test")
    ASSERT(splicetext_char("", 4, 5, "test") == "test")