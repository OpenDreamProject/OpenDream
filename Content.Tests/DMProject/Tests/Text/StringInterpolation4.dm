/obj/test1
    name = ""
/obj/test2
    name = "   " // 3 spaces
/obj/test3
    name = "\t"

/proc/RunTest()
    var/list/correct = list(
        "/obj/test1: ",
        "/obj/test2:    ",
        "/obj/test3: \t"
    )
    var/i = 1
    for (var/T in typesof(/obj))
        if(T == /obj)
            continue
        var/obj/O = new T()
        var/true_text = correct[i]
        ASSERT(true_text == "[T]: \the [O]")
        ++i
