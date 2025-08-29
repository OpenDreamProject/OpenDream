/mob/proc/test()
    return

/world/New()
    ..()
    var/mob/m = new
    m.verbs += /mob/proc/test
    m.verbs += /mob/proc/test
    ASSERT(m.verbs.len == 1)
