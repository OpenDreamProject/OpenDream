/mob/find_test/verb/hello()
	set src = usr
	usr << "hi"

/datum/unit_test/special_list_find/proc/find_in_args(a, b, c)
	return args.Find(b)

/datum/unit_test/special_list_find/RunTest()
	var/turf/T = locate(2, 2, 1)
	var/obj/first = new(T)
	var/obj/second = new(T)

	// turf.contents
	ASSERT(T.contents.Find(first) == 1)
	ASSERT(T.contents.Find(second) == 2)
	ASSERT(T.contents.Find(second, 1, 2) == 0)
	ASSERT(T.contents.Find(second, 3) == 0)
	ASSERT(T.contents.Find(new /obj) == 0)
	// contents lists ignore an out-of-range End by default
	ASSERT(T.contents.Find(second, 1, 99) == 2)
	ASSERT(T.contents.Find(second, 0) == 2)
	ASSERT(T.contents.Find(second, -1) == 0)

	// obj.contents
	var/obj/holder = new(T)
	var/obj/held = new(holder)
	ASSERT(holder.contents.Find(held) == 1)
	ASSERT(holder.contents.Find(held, 1, 99) == 1)
	ASSERT(holder.contents.Find(second) == 0)

	// area.contents, its turfs and their contents
	var/area/A = T.loc
	ASSERT(A.contents.Find(T) > 0)
	ASSERT(A.contents.Find(second) > A.contents.Find(T))
	ASSERT(A.contents.Find(second, 1, 9999) == A.contents.Find(second))

	// world.contents
	ASSERT(world.contents.Find(second) > 0)
	ASSERT(world.contents.Find(T) > 0)

	// overlays only match appearances read back out of the list
	first.overlays += "somestate"
	var/image/overlayImage = new()
	overlayImage.icon_state = "other"
	first.overlays += overlayImage
	ASSERT(length(first.overlays) == 2)
	ASSERT(first.overlays.Find(first.overlays[1]) == 1)
	ASSERT(first.overlays.Find(first.overlays[2]) == 2)
	ASSERT(first.overlays.Find(first.overlays[2], 2) == 2)
	ASSERT(first.overlays.Find(first.overlays[2], 1, 2) == 0)
	ASSERT(first.overlays.Find("somestate") == 0)
	ASSERT(first.overlays.Find(overlayImage) == 0)
	ASSERT(first.overlays.Find(null) == 0)
	// equal appearances match across atoms
	second.overlays += "somestate"
	ASSERT(first.overlays.Find(second.overlays[1]) == 1)
	// `in` matches the same things Find() does
	ASSERT(first.overlays[1] in first.overlays)
	ASSERT(!("somestate" in first.overlays))
	ASSERT(!(overlayImage in first.overlays))

	// BYOND's filters list matches nothing at all, unfortunately
	first.filters += filter(type = "blur", size = 2)
	ASSERT(length(first.filters) == 1)
	ASSERT(!(first.filters[1] in first.filters))

	first.underlays += "understate"
	ASSERT(first.underlays.Find(first.underlays[1]) == 1)
	ASSERT(first.underlays[1] in first.underlays)
	ASSERT(first.underlays.Find("understate") == 0)

	// vis_contents
	first.vis_contents += second
	ASSERT(first.vis_contents.Find(second) == 1)
	ASSERT(first.vis_contents.Find(holder) == 0)

	// verbs
	var/mob/find_test/M = new(T)
	ASSERT(M.verbs.Find(/mob/find_test/verb/hello) == 1)
	ASSERT(M.verbs.Find(M.verbs[1]) == 1)

	// savefile.dir also ignores an out-of-range End
	var/savefile/S = new("special_list_find.sav")
	S.dir.Add("alpha")
	S.dir.Add("beta")
	ASSERT(S.dir.Find("alpha") == 1)
	ASSERT(S.dir.Find("beta") == 2)
	ASSERT(S.dir.Find("gamma") == 0)
	ASSERT(S.dir.Find("alpha", 1, 99) == 1)
	ASSERT(S.dir.Find("alpha", -1) == 0)

	// vars, global.vars and args
	var/list/varsList = first.vars
	var/nameIndex = varsList.Find("name")
	ASSERT(nameIndex > 0)
	ASSERT(varsList.Find("name", nameIndex + 1) == 0)
	ASSERT(varsList.Find("name", 1, nameIndex) == 0)
	ASSERT(varsList.Find("not_a_var") == 0)
	ASSERT(global.vars.Find("areas_by_type") > 0)
	ASSERT(global.vars.Find("definitely_not_a_global") == 0)
	ASSERT(find_in_args("a", "b", "c") == 2)

	// Start rules, same on every list
	var/list/L = list("a", "b", "c")
	ASSERT(L.Find("a", 0) == 1) // Start 0 counts as 1
	ASSERT(L.Find("a", -1) == 0) // negative Start finds nothing
	ASSERT(L.Find("c", 1, 4) == 3) // End of len+1 is the last valid position
