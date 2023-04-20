proc/RunTest()
    var/A = @{"\n\""}
    var/B = "}"
    ASSERT(A == "\\n\\\"")