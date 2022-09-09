
#define ISDEF 0
#ifdef ISDEF
#define LOG_DEF 1
#else
# define LOG_DEF 0
#endif

/proc/RunTest()
	ASSERT(LOG_DEF)
