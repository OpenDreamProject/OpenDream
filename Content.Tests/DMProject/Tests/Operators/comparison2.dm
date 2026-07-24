/proc/comparison_reference_proc_a()
	return

/proc/comparison_reference_proc_b()
	return

/proc/assert_left_preserving_comparisons(label, left, right)
	var/result = left < right
	if (result != left)
		CRASH("[label] < expected the left operand, got [result]")

	result = left <= right
	if (result != left)
		CRASH("[label] <= expected the left operand, got [result]")

	result = left > right
	if (result != left)
		CRASH("[label] > expected the left operand, got [result]")

	result = left >= right
	if (result != left)
		CRASH("[label] >= expected the left operand, got [result]")

/proc/RunTest()
	var/datum/datum_a = new
	var/datum/datum_b = new
	var/list/list_a = list(1)
	var/list/list_b = list(1)
	var/typepath_a = /datum
	var/typepath_b = /atom
	var/procpath_a = /proc/comparison_reference_proc_a
	var/procpath_b = /proc/comparison_reference_proc_b
	var/icon_a = icon()
	var/icon_b = icon()
	var/sound_a = sound()
	var/sound_b = sound()
	var/file_a = file("comparison_probe_a.tmp")
	var/file_b = file("comparison_probe_b.tmp")

	var/list/reference_names = list("datum", "list", "typepath", "procpath", "icon", "sound", "file")
	var/list/reference_values = list(datum_a, list_a, typepath_a, procpath_a, icon_a, sound_a, file_a)
	var/list/left_names = reference_names + list("num", "text", "null")
	var/list/left_values = reference_values + list(1, "abc", null)

	for (var/left_index in 1 to left_values.len)
		for (var/right_index in 1 to reference_values.len)
			assert_left_preserving_comparisons(
				"[left_names[left_index]]_[reference_names[right_index]]",
				left_values[left_index],
				reference_values[right_index]
			)

	assert_left_preserving_comparisons("distinct datums", datum_a, datum_b)
	assert_left_preserving_comparisons("distinct lists", list_a, list_b)
	assert_left_preserving_comparisons("distinct typepaths", typepath_a, typepath_b)
	assert_left_preserving_comparisons("distinct proc paths", procpath_a, procpath_b)
	assert_left_preserving_comparisons("distinct icons", icon_a, icon_b)
	assert_left_preserving_comparisons("distinct sounds", sound_a, sound_b)
	assert_left_preserving_comparisons("distinct files", file_a, file_b)
