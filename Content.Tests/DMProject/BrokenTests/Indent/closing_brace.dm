
//# issue 512

/proc/closing_brace1() {
	var/z = 1
	if (z) {
		. = ++z
		}
	}

/proc/RunTest()
	ASSERT(closing_brace1() == 2)
