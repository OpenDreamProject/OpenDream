
/proc/RunTest()
    var/list/one = list("a"=1, "b"=2, "c"=-3)
    ASSERT(values_cut_under(one, 5, 1) == 3)
    ASSERT(one ~= list())

    var/list/two = list("a"=1, "b"=2, "c"=0)
    ASSERT(values_cut_under(two, 1) == 1)
    ASSERT(two ~= list("a"=1, "b"=2))

    var/list/three = list("a"=1, "b"=2, "c"=0)
    ASSERT(values_cut_under(three, 1, TRUE) == 2)
    ASSERT(three ~= list("b" = 2))

    var/list/four = list("a"=1, "b"=2, "c"=-3)
    ASSERT(values_cut_over(four, 5, 1) == 0)
    ASSERT(four ~= list("a"=1, "b"=2, "c"=-3))

    var/list/five = list("a"=1, "b"=2, "c"=0)
    ASSERT(values_cut_over(five, 1) == 1)
    ASSERT(five ~= list("a"=1, "c"=0))

    var/list/six = list("a"=1, "b"=2, "c"=0)
    ASSERT(values_cut_over(six, 1, TRUE) == 2)
    ASSERT(six ~= list("c" = 0))

    var/list/seven = list("a", "b", "c")
    ASSERT(values_cut_under(seven, -1, 0) == 3)
    ASSERT(seven ~= list())

    var/list/eight = list("a"=1, "b", "c"=-3)
    ASSERT(values_cut_under(eight, -1, 0) == 2)
    ASSERT(eight ~= list("a" = 1))
