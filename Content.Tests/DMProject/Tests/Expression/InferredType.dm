/proc/RunTest()
    var/matrix/M1 = new()
    var/matrix/M2 = new()

    (M1 * M2).Multiply(1)

    var/list/test = list(M1,M2)
    (locate(/matrix) in test).Multiply(1)