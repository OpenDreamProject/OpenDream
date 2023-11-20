#define REG_NOTBB "\[^\\\[\]+"

/proc/RunTest()
	test_regex_bb("span")
	test_regex_bb("style")
	test_regex_bb("class")
	test_regex_bb("bold")

/proc/REG_BBTAG(x)
	return "\\\[[x]\\\]"

/proc/REG_BETWEEN_BBTAG(x)
	return "[REG_BBTAG(x)]([REG_NOTBB])[REG_BBTAG("/[x]")]"

/world/proc/test_regex_bb(bbtag)
	var/regexp = regex(REG_BETWEEN_BBTAG(bbtag))

	var/anti_tag = bbtag == "style" ? "span" : "style"
	var/inner = "text which is inside this!!."
	var/not = "\[[anti_tag]\][inner]\[/[anti_tag]\]"
	var/yes = "\[[bbtag]\][inner]\[/[bbtag]\]"

	ASSERT(!findtext(not, regexp))
	ASSERT(findtext(yes, regexp))
