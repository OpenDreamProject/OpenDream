/proc/RunTest()
    var/obj/obj1 = new()
    var/obj/obj2 = new()
    var/list/things = list(obj1,"beep","thing","stuff")
    var/A = locate(ref(obj1)) in things
    ASSERT(A == obj1)
    var/B = locate(ref(obj2)) in things
    ASSERT(isnull(B))