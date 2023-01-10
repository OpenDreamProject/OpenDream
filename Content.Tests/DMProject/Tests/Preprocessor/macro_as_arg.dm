#define DUMMY(lol) inc_count_and_assert(lol)
#define YMMUD(lol) dec_count_and_assert(lol)
#define _GETTER_4(_, _, _, a, ...) a
#define _GETTER_4_OR_DUMMY(args...) _GETTER_4(##args, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY)

var/i = 0
/proc/inc_count_and_assert(a)
 	if(a == 2)
		i++

/proc/dec_count_and_assert(a)
	if(a==2)
		i--

/proc/RunTest()
	_GETTER_4_OR_DUMMY(1,2,3)(2)
	ASSERT(i == 1)
	_GETTER_4_OR_DUMMY(1,2,3,DUMMY)(2)
	ASSERT(i == 2)
	_GETTER_4_OR_DUMMY(1,2,3,YMMUD)(2)
	ASSERT(i == 1)