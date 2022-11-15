
//# issue 679

/obj/step/ty
/obj/throw/ty
/obj/null/ty
/obj/switch/ty
/obj/spawn/ty

/proc/RunTest()
	var/obj/throw/o = new
	ASSERT(istype(o, /obj/throw))
