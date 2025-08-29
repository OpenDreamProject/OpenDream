/obj/blurry
    filters = filter(type="blur", size=2)

/obj/veryblurry
    filters = list(type="blur", size=4)

/obj/notatallblurry
    filters = list()

/world/New()
    ..()
    var/obj/veryblurry/VB = new()
    ASSERT(length(VB.filters) == 1)
    var/obj/blurry/B = new()
    ASSERT(length(B.filters) == 1)
    var/obj/notatallblurry/NAB = new()
    ASSERT(length(NAB.filters) == 0)