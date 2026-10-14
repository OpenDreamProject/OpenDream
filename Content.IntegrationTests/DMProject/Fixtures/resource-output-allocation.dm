/proc/output_savefile_roundtrip()
    var/savefile/F = new("output-roundtrip.sav")
    var/text = "Unicode é 😀 " + ascii2text(65280) + " literal " + ascii2text(13) + "\n line"
    for (var/i = 1, i <= 4097, i++)
        text += "x"
    F["text"] << text
    F["number"] << 17.5
    F["nested"] << list("alpha", "beta", "named" = list(1, 2))
    F.Flush()
    var/savefile/G = new("output-roundtrip.sav")
    var/read_text
    var/read_number
    G["text"] >> read_text
    G["number"] >> read_number
    var/list/read_list
    G["nested"] >> read_list
    var/ok = read_text == text && read_number == 17.5 && read_list[1] == "alpha" && read_list["named"][2] == 2
    del(F)
    del(G)
    var/savefile/H = new("output-roundtrip.sav")
    H["text"] >> read_text
    ok = ok && read_text == text
    del(H)
    return ok
