
//# issue 686

/proc/RunTest()
	if(1) { \
		if(2) { \
			ASSERT(TRUE); \
		}; \
	}; \
	else { \
		ASSERT(FALSE); \
	}
