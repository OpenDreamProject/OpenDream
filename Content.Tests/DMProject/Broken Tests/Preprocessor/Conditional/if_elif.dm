
//# issue 318

#define B 2
#if B == 1
#define A 1
#elif B == 2
#define A 2
#endif

/proc/RunTest()
	ASSERT(A == 2)
