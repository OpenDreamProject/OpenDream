/world/New()
    ..()
    ASSERT(bounds_dist(locate(3, 3, 2), locate(3, 3, 2)) == -32)
    ASSERT(isinf(bounds_dist(locate(3, 3, 2), locate(3, 3, 3))))
    for (var/turf/T in orange(1, locate(3, 3, 2)))
        ASSERT(bounds_dist(locate(3, 3, 2), T == 0))
    
    for (var/turf/T in (orange(2, locate(3, 3, 2)) - orange(1, locate(3, 3, 2))))
        ASSERT(bounds_dist(locate(3, 3, 2), T == 32))