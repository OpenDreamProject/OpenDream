
//# issue 366

#define MCRO(a) a

/proc/RunTest()
	var/input = MCRO(@{""|[\\\n\t/?%*:|<>]"})
	var/expected = "\"|\[\\\\\\n\\t/?%*:|<>]"
	ASSERT(input == expected)