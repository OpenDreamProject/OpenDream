/datum/test/var/name

/datum/test/test1
    name = ""
/datum/test/test2
    name = "   " // 3 spaces
/datum/test/test3
    name = "\t"

/proc/RunTest()
    var/list/correct = list(
        "/datum/test/test1: ",
        "/datum/test/test2:    ",
        "/datum/test/test3: \t"
    )
    var/i = 1
    for (var/T in typesof(/datum/test))
        if(T == /datum/test)
            continue
        var/datum/test/D = new T()
        var/true_text = correct[i]
        ASSERT(true_text == "[T]: \the [D]")
        ++i
