
//# issue 995

#define NESTED_TRY_CATCH_ONELINER(x, a) \
	try { throw x; } catch(var/e1) { a = e1; }

#define NESTED_TRY_CATCH(x, a) \
	try { \
		throw x; \
	} catch(var/e2) { \
		 a = e2; \
	}

/proc/RunTest()
	var/a
	NESTED_TRY_CATCH_ONELINER(5, a)
	ASSERT(a == 5)
	NESTED_TRY_CATCH(10, a)
	ASSERT(a == 10)
