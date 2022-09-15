/proc/RunTest()
    file("/home/amy/dmdebug.txt") << "Spantext tests"
    ASSERT(spantext("apples, oranges",", ",7) == 2)
    ASSERT(spantext("","b",1) == 0)
    ASSERT(spantext("a","",1) == 0)
    ASSERT(spantext("aaaaba","bb",5) == 1)
    ASSERT(spantext("aaa","a",1) == 3)
    ASSERT(spantext("aaa","a",-1) == 1)
    ASSERT(spantext("aaa","a",-4) == 3)
    //spantext_char tests
    file("/home/amy/dmdebug.txt") << "Spantext_char tests"
    file("/home/amy/dmdebug.txt") << num2text(spantext_char("apples, oranges",", ",7))
    file("/home/amy/dmdebug.txt") << num2text(spantext("aa𐀀𐀀bb", "b", 5))
    file("/home/amy/dmdebug.txt") << num2text(spantext_char("aa𐀀𐀀bb", "b", 5))
    file("/home/amy/dmdebug.txt") << num2text(spantext_char("aaa","a",-4))
    file("/home/amy/dmdebug.txt") << num2text(spantext("aa𐀀𐀀bb", "𐀀",3))
    file("/home/amy/dmdebug.txt") << num2text(spantext_char("aa𐀀𐀀bb", "𐀀",3))
    ASSERT(spantext_char("apples, oranges",", ",7) == 2)
    ASSERT(spantext("aa𐀀𐀀bb", "b", 5) == 0)
    ASSERT(spantext_char("aaa","a",-4) == 3)
    ASSERT(spantext("aa𐀀𐀀bb", "𐀀",3) == 4) //DM evaluates this as 8 and the next as 4. Because it mangles
    ASSERT(spantext_char("aa𐀀𐀀bb", "𐀀",3) == 2) //the chars. We instead want 4 and 2 respectively.