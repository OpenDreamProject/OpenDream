
//# issue 700

#define REG_NOTBB "\[^\\\[\]+"    // [^\]]+

/proc/REG_BBTAG(x)
	return "\\\[[x]\\\]"

// [x]blah[/x]
/proc/REG_BETWEEN_BBTAG(x)
	return "[REG_BBTAG(x)]([REG_NOTBB])[REG_BBTAG("/[x]")]"

/proc/RunTest()
	var/input = (REG_BETWEEN_BBTAG("BB"))
	var/expected = "\\\[BB\\\](\[^\\\[]+)\\\[/BB\\\]"
	ASSERT(expected == input)
