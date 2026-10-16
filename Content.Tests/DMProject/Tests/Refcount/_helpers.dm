// Effectively removes any values relevant to a test from byond's internal variable cache.
#ifdef OPENDREAM
#define BYOND_POP_CACHE
#else
#define BYOND_POP_CACHE \
	do { \
		var/foo = 1; \
		foo += 1; \
	} while(FALSE);
#endif