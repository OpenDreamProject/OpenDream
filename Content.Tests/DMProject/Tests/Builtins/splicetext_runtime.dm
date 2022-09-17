// RUNTIME ERROR

/proc/RunTest()
    splicetext("banana", 12, -1, "test") //bad text or out of bounds
    splicetext("banana", 3, -5, "test") //bad text or out of bounds
    splicetext("banana", 0, 6, "laclav") //bad text or out of bounds
    splicetext("abcdef", 4, 3, "test") //bad text or out of bounds
