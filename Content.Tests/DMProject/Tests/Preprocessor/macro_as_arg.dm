#define DUMMY(lol) inc_count_and_assert(lol)

#define _GETTER_3(_, _, a, ...) a
#define TUPLE_GET_3(x) x(_GETTER_3)
#define GET_MACRO_ARG TUPLE_GET_3

#define _GETTER_4(_, _, _, a, ...) a
#define _GETTER_4_OR_DUMMY(args...) _GETTER_4(##args, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY, DUMMY)
#define TUPLE_GET_4_OR_DUMMY(x) x(_GETTER_4_OR_DUMMY)
#define GET_MACRO_OR_DUMMY TUPLE_GET_4_OR_DUMMY

#define DO_THING(thing, thing_arg) do { thing(thing_arg) } while(0)

#define APPLY_ATOM_PROPERTY(_, property) DO_THING(GET_MACRO_OR_DUMMY(property), GET_MACRO_ARG(property))

#define PROP_TEST(x) x("first", DO_THING, 2)
#define PROP_TEST_2(x) x("first", DO_THING, 2, DUMMY)

var/i = 0

/proc/inc_count_and_assert(a)
	if(a == 2)
		i++

/proc/RunTest()
	APPLY_ATOM_PROPERTY(null, PROP_TEST)
	if(i != 1)
		CRASH("Test failed at PROP_TEST")
	APPLY_ATOM_PROPERTY(null, PROP_TEST_2)
	if(i != 2)
		CRASH("Test failed at PROP_TEST_2")