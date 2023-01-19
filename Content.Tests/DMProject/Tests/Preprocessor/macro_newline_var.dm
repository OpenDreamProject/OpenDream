#define DEFINE_FLOORS(_PATH, _VARS) \
	/obj/simulated/floor/_PATH{_VARS};\
	/obj/unsimulated/floor/_PATH{_VARS};\
	/obj/simulated/floor/airless/_PATH{_VARS};\
	/obj/unsimulated/floor/airless/_PATH{_VARS};

var/list/gvars_datum_init_order = list()

#define GLOBAL_RAW(X) /var/global##X
#define GLOBAL_MANAGED(X, InitValue)\
/proc/InitGlobal##X(){\
	##X = ##InitValue;\
	gvars_datum_init_order += #X;\
}
#define GLOBAL_LIST_INIT(X, InitValue) GLOBAL_RAW(/list/##X); GLOBAL_MANAGED(X, InitValue)

DEFINE_FLOORS(carpet/regalcarpet,
	name = "regal carpet";\
	icon_state = "regal_carpet";)

DEFINE_FLOORS(carpet/regalcarpet/border,
	name = "regal carpet border";\
	icon_state = "regal_carpet";)

GLOBAL_LIST_INIT(test, list(
	1,
	2,
	3,
	4
	))
GLOBAL_LIST_INIT(test2, list(
	1,
	#ifdef NOTDEF
	2,
	3,
	#endif
	4
	))	

proc/RunTest()
	InitGlobaltest()
	ASSERT(test.len == 4)	
	InitGlobaltest2()
	ASSERT(test2.len == 2)
	var/obj/simulated/floor/carpet/regalcarpet/C1 = new()
	var/obj/simulated/floor/carpet/regalcarpet/border/C2 = new()
	ASSERT(C1.name == "regal carpet")
	ASSERT(C2.name == "regal carpet border")
	
