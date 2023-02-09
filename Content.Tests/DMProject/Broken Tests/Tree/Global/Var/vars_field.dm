
//# issue 697

// TODO: A lot of macros seem to also be in global.vars in BYOND but this isn't implemented in OD yet

/proc/RunTest()
	ASSERT(global.vars["TRUE"] == 1)
