#define DEFINE_FLOORS(_PATH, _VARS) \
	/obj/simulated/floor/_PATH{_VARS};\
	/obj/unsimulated/floor/_PATH{_VARS};\
	/obj/simulated/floor/airless/_PATH{_VARS};\
	/obj/unsimulated/floor/airless/_PATH{_VARS};

DEFINE_FLOORS(carpet/regalcarpet,
	name = "regal carpet";\
	icon = 'icons/obj/floors.dmi';\
	icon_state = "regal_carpet";)

DEFINE_FLOORS(carpet/regalcarpet/border,
	name = "regal carpet border";\
	icon = 'icons/obj/floors.dmi';\
	icon_state = "regal_carpet";)

proc/RunTest()
	var/obj/simulated/floor/carpet/regalcarpet/C1 = new()
	var/obj/simulated/floor/carpet/regalcarpet/border/C2 = new()
	ASSERT(C1.name == "regal carpet")
	ASSERT(C2.name == "regal carpet border")
	