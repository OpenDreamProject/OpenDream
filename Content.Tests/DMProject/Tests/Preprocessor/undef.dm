#define BUH "buh buh buh buh buh"

var/const/buh = BUH

#undef BUH

#ifdef BUH
#error "buh didnt get undefined!"
#endif

/proc/RunTest()
	ASSERT(buh == "buh buh buh buh buh")
