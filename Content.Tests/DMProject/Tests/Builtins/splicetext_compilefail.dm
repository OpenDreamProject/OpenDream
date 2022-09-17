 // COMPILE ERROR

/proc/RunTest()
    splicetext("abcdef", 4, Insert="test") //expected 2 to 4 arguments    
    splicetext("abcdef", Insert="") //expected 2 to 4 arguments
    splicetext("abcdef") //expected 2 to 4 arguments