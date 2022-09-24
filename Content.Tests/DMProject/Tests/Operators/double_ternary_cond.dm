
/obj
    var/a
    var/b

/proc/ternary(name, obj/o, a, b)
    o.a = a
    o.b = b
    return (o.a ? "str":o.b ? "a":"b")

/proc/RunTest()
    var/obj/o = new

    ASSERT(ternary("?00", o, 0, 0) == "b")
    ASSERT(ternary("?01", o, 0, 1) == "a")
    ASSERT(ternary("?10", o, 1, 0) == "str")
    ASSERT(ternary("?11", o, 1, 1) == "str")
