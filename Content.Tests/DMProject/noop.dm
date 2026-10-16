/// A function that does absolutely nothing. Needed for behavior like clearing the variable cache in byond.
/proc/noop()
	return

#ifdef OPENDREAM
#define BYOND_POP_CACHE
#else
#define BYOND_POP_CACHE noop()
#endif