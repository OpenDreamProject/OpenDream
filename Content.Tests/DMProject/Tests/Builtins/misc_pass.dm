// RETURN TRUE

/obj/builtin_probe

/proc/builtin_noop()
	return

/proc/RunTest()
	ASSERT(gradient("#ff0000", 0.5) == "#ff0000")
	ASSERT(rgb(1, 2, 3, null, 99) == "#01020300")
	ASSERT(length(/proc/builtin_noop) == 0)

	var/obj/builtin_probe/probe = new
	ASSERT(isnull(animate(probe, time = 1, easing = 999)))
	ASSERT(isnull(get_step(probe, "bad")))
	ASSERT(isnull(locate("bad", "coordinates", "here")))
	var/bad_container_result = locate("missing") in "not a container"
	ASSERT(isnull(bad_container_result))
	ASSERT(get_dir("not a location", 42) == 0)
	return TRUE
