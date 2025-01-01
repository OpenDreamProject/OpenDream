
// TODO This test needs to be fixed up and tested in actual BYOND using the unit test DME, not a discord compiler bot, and I cba

/proc/main()
    var/list/forvals = list()

    forvals = list()
    for (var/datum/X)
        forvals += X
    LOG("datum", forvals)

    forvals = list()
    for (var/atom/X)
        forvals += X
    LOG("atom", forvals)

    forvals = list()
    for (var/area/X)
        forvals += X
    LOG("area", forvals)

    forvals = list()
    for (var/turf/X)
        forvals += X
    LOG("turf", forvals)

    forvals = list()
    for (var/mob/X)
        forvals += X
    LOG("mob", forvals)

    forvals = list()
    for (var/obj/X)
        forvals += X
    LOG("obj", forvals)

    forvals = list()
    for (var/atom/movable/X)
        forvals += X
    LOG("atom/movable", forvals)