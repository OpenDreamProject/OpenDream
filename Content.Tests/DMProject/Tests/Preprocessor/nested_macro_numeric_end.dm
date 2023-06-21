#define APPLY_PREFIX(prefix, ARGS...) _APPLY_PREFIX(prefix, ##ARGS, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0)
#define _APPLY_PREFIX(prefix, a, b, c, d, e, f, g, h, i, j, ...) _APPLY_PREFIX_##j(prefix, a, b, c, d, e, f, g, h, i, j)
#define _APPLY_PREFIX_0(prefix, a, b, c, d, e, f, g, h, i, j, ...)
#define _APPLY_PREFIX_1(prefix, a, b, c, d, e, f, g, h, i, j, ...) prefix##a
#define _APPLY_PREFIX_2(prefix, a, b, c, d, e, f, g, h, i, j, ...) prefix##a, _APPLY_PREFIX_1(prefix, b, c, d, e, f, g, h, i, j, -1)
#define TEST_CASE(TYPE, PROCNAME...) var/testlist = list(APPLY_PREFIX(TYPE/, PROCNAME))

TEST_CASE(/obj/critter/domestic_bee, proc/dance, proc/puke_honey)

/obj/critter/domestic_bee
	proc/dance()
		return "DANCE"

	proc/puke_honey()
		return "HONEY"

proc/RunTest()
	return 0