/obj
  var/list/type_list = null

/obj/subtype/broken
  type_list = null

/obj/subtype
  type_list = list(/datum, /atom, /obj,)

/proc/RunTest()
  var/obj/O = new /obj/subtype/broken()
  ASSERT(isnull(O.type_list))