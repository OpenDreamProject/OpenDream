
// TODO This test needs to be fixed up and tested in actual BYOND using the unit test DME, not a discord compiler bot, and I cba

/proc/main()

    var/list/forvals = list()
    for (var/X as anything)
        forvals += X
    LOG("anything", forvals)

    forvals = list()
    for (var/X as area)
        forvals += X
    LOG("area", forvals)

    forvals = list()
    for (var/X as turf)
        forvals += X
    LOG("turf", forvals)

    forvals = list()
    for (var/X as obj)
        forvals += X
    LOG("obj", forvals)

    forvals = list()
    for (var/X as mob)
        forvals += X
    LOG("mob", forvals)

    forvals = list()
    for (var/X as num)
        forvals += X
    LOG("num", forvals)